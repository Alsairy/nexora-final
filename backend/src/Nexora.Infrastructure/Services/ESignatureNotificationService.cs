using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Data;
using Nexora.Core.Models;

namespace Nexora.Infrastructure.Services
{
    public class ESignatureNotificationService : IESignatureNotificationService
    {
        private readonly NexoraDbContext _context;
        private readonly ILogger<ESignatureNotificationService> _logger;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly INotificationHub _notificationHub;
        private readonly IAuditService _auditService;

        public ESignatureNotificationService(
            NexoraDbContext context,
            ILogger<ESignatureNotificationService> logger,
            IEmailService emailService,
            ISmsService smsService,
            INotificationHub notificationHub,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _smsService = smsService;
            _notificationHub = notificationHub;
            _auditService = auditService;
        }

        public async Task SendSigningInvitationAsync(int documentId, int signerId, string customMessage, int tenantId)
        {
            try
            {
                _logger.LogInformation("Sending signing invitation for document {DocumentId} to signer {SignerId}", documentId, signerId);

                var document = await _context.ESignatureDocuments
                    .Include(d => d.CreatedByUser)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                var signer = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (document == null || signer == null)
                {
                    _logger.LogWarning("Document or signer not found for invitation");
                    return;
                }

                var signingLink = await GenerateSigningLinkAsync(documentId, signerId, tenantId);

                var notification = new ESignatureNotification
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    SignerId = signerId,
                    NotificationType = "SigningInvitation",
                    Channel = "Email",
                    RecipientEmail = signer.Email,
                    RecipientPhone = signer.PhoneNumber,
                    Subject = $"Document Signing Request: {document.Title}",
                    Message = customMessage ?? GenerateDefaultInvitationMessage(document, signer, signingLink),
                    Status = "Pending",
                    ScheduledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ESignatureNotifications.Add(notification);
                await _context.SaveChangesAsync();

                if (signer.EmailNotificationsEnabled && !string.IsNullOrEmpty(signer.Email))
                {
                    var emailResult = await _emailService.SendEmailAsync(
                        signer.Email,
                        notification.Subject,
                        GenerateInvitationEmailHtml(document, signer, signingLink, customMessage)
                    );

                    notification.EmailSent = emailResult == "SUCCESS";
                    notification.EmailSentAt = emailResult == "SUCCESS" ? DateTime.UtcNow : null;
                    notification.EmailError = emailResult == "SUCCESS" ? null : emailResult;
                }

                if (signer.SmsNotificationsEnabled && !string.IsNullOrEmpty(signer.PhoneNumber))
                {
                    var smsMessage = GenerateInvitationSmsMessage(document, signer, signingLink);
                    var smsResult = await _smsService.SendSmsAsync(signer.PhoneNumber, smsMessage);

                    notification.SmsSent = smsResult == "SUCCESS";
                    notification.SmsSentAt = smsResult == "SUCCESS" ? DateTime.UtcNow : null;
                    notification.SmsError = smsResult == "SUCCESS" ? null : smsResult;
                }

                await _notificationHub.SendNotificationToUserAsync(signer.Email, "You have been invited to sign a document");

                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(new Nexora.Core.Models.AuditEntry
                {
                    TenantId = tenantId.ToString(),
                    EntityName = nameof(ESignatureNotification),
                    EntityId = notification.Id.ToString(),
                    Action = "SendInvitation",
                    NewValues = new Dictionary<string, object?> { ["DocumentId"] = documentId, ["SignerId"] = signerId },
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signing invitation for document {DocumentId} to signer {SignerId}", documentId, signerId);
                throw;
            }
        }

        public async Task SendReminderNotificationAsync(int documentId, int signerId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Sending reminder notification for document {DocumentId} to signer {SignerId}", documentId, signerId);

                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                var signer = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (document == null || signer == null || signer.Status == "Signed")
                {
                    _logger.LogWarning("Document, signer not found or already signed for reminder");
                    return;
                }

                var signingLink = await GenerateSigningLinkAsync(documentId, signerId, tenantId);

                var notification = new ESignatureNotification
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    SignerId = signerId,
                    NotificationType = "Reminder",
                    Channel = "Email",
                    RecipientEmail = signer.Email,
                    RecipientPhone = signer.PhoneNumber,
                    Subject = $"Reminder: Document Signing Required - {document.Title}",
                    Message = GenerateReminderMessage(document, signer, signingLink),
                    Status = "Pending",
                    ScheduledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ESignatureNotifications.Add(notification);
                await _context.SaveChangesAsync();

                if (signer.EmailNotificationsEnabled && !string.IsNullOrEmpty(signer.Email))
                {
                    var emailResult = await _emailService.SendEmailAsync(
                        signer.Email,
                        notification.Subject,
                        GenerateReminderEmailHtml(document, signer, signingLink)
                    );

                    notification.EmailSent = emailResult == "SUCCESS";
                    notification.EmailSentAt = emailResult == "SUCCESS" ? DateTime.UtcNow : null;
                    notification.EmailError = emailResult == "SUCCESS" ? null : emailResult;
                }

                if (signer.SmsNotificationsEnabled && !string.IsNullOrEmpty(signer.PhoneNumber))
                {
                    var smsMessage = GenerateReminderSmsMessage(document, signer, signingLink);
                    var smsResult = await _smsService.SendSmsAsync(signer.PhoneNumber, smsMessage);

                    notification.SmsSent = smsResult == "SUCCESS";
                    notification.SmsSentAt = smsResult == "SUCCESS" ? DateTime.UtcNow : null;
                    notification.SmsError = smsResult == "SUCCESS" ? null : smsResult;
                }

                notification.Status = "Sent";
                notification.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                signer.LastReminderSentAt = DateTime.UtcNow;
                signer.ReminderCount = signer.ReminderCount + 1;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder notification for document {DocumentId} to signer {SignerId}", documentId, signerId);
                throw;
            }
        }

        public async Task SendDocumentCompletedNotificationAsync(int documentId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Sending document completed notification for document {DocumentId}", documentId);

                var document = await _context.ESignatureDocuments
                    .Include(d => d.CreatedByUser)
                    .Include(d => d.Signers)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                {
                    _logger.LogWarning("Document not found for completion notification");
                    return;
                }

                if (document.CreatedByUser != null)
                {
                    await SendCompletionNotificationToUser(document, document.CreatedByUser, "DocumentOwner", tenantId);
                }

                foreach (var signer in document.Signers.Where(s => s.Status == "Signed"))
                {
                    await SendCompletionNotificationToSigner(document, signer, tenantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document completed notification for document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task SendDocumentExpiredNotificationAsync(int documentId, int tenantId)
        {
            try
            {
                _logger.LogInformation("Sending document expired notification for document {DocumentId}", documentId);

                var document = await _context.ESignatureDocuments
                    .Include(d => d.CreatedByUser)
                    .Include(d => d.Signers)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                if (document == null)
                {
                    _logger.LogWarning("Document not found for expiry notification");
                    return;
                }

                if (document.CreatedByUser != null)
                {
                    await SendExpiryNotificationToUser(document, document.CreatedByUser, tenantId);
                }

                foreach (var signer in document.Signers.Where(s => s.Status != "Signed"))
                {
                    await SendExpiryNotificationToSigner(document, signer, tenantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document expired notification for document {DocumentId}", documentId);
                throw;
            }
        }

        private async Task<string> GenerateSigningLinkAsync(int documentId, int signerId, int tenantId)
        {
            var signer = await _context.ESignatureSigners
                .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId);

            if (signer == null)
                return string.Empty;

            return $"https://nexora.sa/esignature/sign/{documentId}?token={signer.SigningToken}&signer={signerId}";
        }

        private string GenerateDefaultInvitationMessage(ESignatureDocument document, ESignatureSigner signer, string signingLink)
        {
            return $@"Dear {signer.FullName},

You have been invited to sign the document ""{document.Title}"".

Please click the following link to review and sign the document:
{signingLink}

If you have any questions, please contact the document sender.

Best regards,
Nexora E-Signature System";
        }

        private string GenerateInvitationEmailHtml(ESignatureDocument document, ESignatureSigner signer, string signingLink, string customMessage)
        {
            var message = customMessage ?? GenerateDefaultInvitationMessage(document, signer, signingLink);
            
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c5aa0;'>Document Signing Request</h2>
                        <p>Dear {signer.FullName},</p>
                        <p>{message}</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{signingLink}' style='background-color: #2c5aa0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                                Sign Document
                            </a>
                        </div>
                        <p style='font-size: 12px; color: #666;'>
                            This link will expire on {DateTime.UtcNow.AddDays(30):yyyy-MM-dd}. 
                            If you cannot click the link, copy and paste it into your browser.
                        </p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666;'>
                            This email was sent by Nexora E-Signature System. 
                            Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";
        }

        private string GenerateInvitationSmsMessage(ESignatureDocument document, ESignatureSigner signer, string signingLink)
        {
            return $"Hi {signer.FullName}, you have been invited to sign \"{document.Title}\". Sign here: {signingLink}";
        }

        private string GenerateReminderMessage(ESignatureDocument document, ESignatureSigner signer, string signingLink)
        {
            return $@"Dear {signer.FullName},

This is a reminder that you have a pending document to sign: ""{document.Title}"".

Please click the following link to review and sign the document:
{signingLink}

If you have already signed this document, please disregard this reminder.

Best regards,
Nexora E-Signature System";
        }

        private string GenerateReminderEmailHtml(ESignatureDocument document, ESignatureSigner signer, string signingLink)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #f39c12;'>Reminder: Document Signing Required</h2>
                        <p>Dear {signer.FullName},</p>
                        <p>This is a reminder that you have a pending document to sign: <strong>{document.Title}</strong></p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{signingLink}' style='background-color: #f39c12; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                                Sign Document Now
                            </a>
                        </div>
                        <p style='font-size: 12px; color: #666;'>
                            If you have already signed this document, please disregard this reminder.
                        </p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666;'>
                            This email was sent by Nexora E-Signature System. 
                            Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";
        }

        private string GenerateReminderSmsMessage(ESignatureDocument document, ESignatureSigner signer, string signingLink)
        {
            return $"Reminder: Please sign \"{document.Title}\". Link: {signingLink}";
        }

        private async Task SendCompletionNotificationToUser(ESignatureDocument document, User user, string role, int tenantId)
        {
            var notification = new ESignatureNotification
            {
                TenantId = tenantId,
                DocumentId = document.Id,
                NotificationType = "DocumentCompleted",
                Channel = "Email",
                RecipientEmail = user.Email,
                Subject = $"Document Completed: {document.Title}",
                Message = $"The document \"{document.Title}\" has been completed and all signatures have been collected.",
                Status = "Sent",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.ESignatureNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        private async Task SendCompletionNotificationToSigner(ESignatureDocument document, ESignatureSigner signer, int tenantId)
        {
            var notification = new ESignatureNotification
            {
                TenantId = tenantId,
                DocumentId = document.Id,
                SignerId = signer.Id,
                NotificationType = "DocumentCompleted",
                Channel = "Email",
                RecipientEmail = signer.Email,
                Subject = $"Document Completed: {document.Title}",
                Message = $"Thank you for signing \"{document.Title}\". The document has been completed.",
                Status = "Sent",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.ESignatureNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        private async Task SendExpiryNotificationToUser(ESignatureDocument document, User user, int tenantId)
        {
            var notification = new ESignatureNotification
            {
                TenantId = tenantId,
                DocumentId = document.Id,
                NotificationType = "DocumentExpired",
                Channel = "Email",
                RecipientEmail = user.Email,
                Subject = $"Document Expired: {document.Title}",
                Message = $"The document \"{document.Title}\" has expired and is no longer available for signing.",
                Status = "Sent",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.ESignatureNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        private async Task SendExpiryNotificationToSigner(ESignatureDocument document, ESignatureSigner signer, int tenantId)
        {
            var notification = new ESignatureNotification
            {
                TenantId = tenantId,
                DocumentId = document.Id,
                SignerId = signer.Id,
                NotificationType = "DocumentExpired",
                Channel = "Email",
                RecipientEmail = signer.Email,
                Subject = $"Document Expired: {document.Title}",
                Message = $"The document \"{document.Title}\" has expired and is no longer available for signing.",
                Status = "Sent",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.ESignatureNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task SendDocumentDeclinedNotificationAsync(int documentId, int signerId, string reason, int tenantId)
        {
            try
            {
                _logger.LogInformation("Sending document declined notification for document {DocumentId} from signer {SignerId}", documentId, signerId);

                var document = await _context.ESignatureDocuments
                    .Include(d => d.CreatedByUser)
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                var signer = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == signerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (document == null || signer == null)
                {
                    _logger.LogWarning("Document or signer not found for decline notification");
                    return;
                }

                var notification = new ESignatureNotification
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    SignerId = signerId,
                    NotificationType = "DocumentDeclined",
                    Channel = "Email",
                    RecipientEmail = document.CreatedByUser?.Email,
                    Subject = $"Document Declined: {document.Title}",
                    Message = $"The document \"{document.Title}\" has been declined by {signer.FullName}. Reason: {reason}",
                    Status = "Sent",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ESignatureNotifications.Add(notification);
                await _context.SaveChangesAsync();

                if (document.CreatedByUser != null && !string.IsNullOrEmpty(document.CreatedByUser.Email))
                {
                    await _emailService.SendEmailAsync(
                        document.CreatedByUser.Email,
                        notification.Subject,
                        GenerateDeclineEmailHtml(document, signer, reason)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document declined notification for document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task SendDocumentDelegatedNotificationAsync(int documentId, int fromSignerId, int toSignerId, string reason, int tenantId)
        {
            try
            {
                _logger.LogInformation("Sending document delegated notification for document {DocumentId} from signer {FromSignerId} to signer {ToSignerId}", documentId, fromSignerId, toSignerId);

                var document = await _context.ESignatureDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

                var fromSigner = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == fromSignerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                var toSigner = await _context.ESignatureSigners
                    .FirstOrDefaultAsync(s => s.Id == toSignerId && s.DocumentId == documentId && s.TenantId == tenantId && !s.IsDeleted);

                if (document == null || fromSigner == null || toSigner == null)
                {
                    _logger.LogWarning("Document or signers not found for delegation notification");
                    return;
                }

                var signingLink = await GenerateSigningLinkAsync(documentId, toSignerId, tenantId);

                var notification = new ESignatureNotification
                {
                    TenantId = tenantId,
                    DocumentId = documentId,
                    SignerId = toSignerId,
                    NotificationType = "DocumentDelegated",
                    Channel = "Email",
                    RecipientEmail = toSigner.Email,
                    Subject = $"Document Delegated to You: {document.Title}",
                    Message = $"The document \"{document.Title}\" has been delegated to you by {fromSigner.FullName}. Reason: {reason}",
                    Status = "Sent",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ESignatureNotifications.Add(notification);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(toSigner.Email))
                {
                    await _emailService.SendEmailAsync(
                        toSigner.Email,
                        notification.Subject,
                        GenerateDelegationEmailHtml(document, fromSigner, toSigner, reason, signingLink)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document delegated notification for document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task ProcessScheduledNotificationsAsync(int tenantId)
        {
            try
            {
                _logger.LogInformation("Processing scheduled notifications for tenant {TenantId}", tenantId);

                var scheduledNotifications = await _context.ESignatureNotifications
                    .Where(n => n.TenantId == tenantId && 
                               n.Status == "Scheduled" && 
                               n.ScheduledAt <= DateTime.UtcNow &&
                               !n.IsDeleted)
                    .ToListAsync();

                foreach (var notification in scheduledNotifications)
                {
                    try
                    {
                        await ProcessScheduledNotification(notification, tenantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing scheduled notification {NotificationId}", notification.Id);
                        notification.Status = "Failed";
                        notification.ErrorMessage = ex.Message;
                    }
                }

                if (scheduledNotifications.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled notifications for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureNotification>> GetDocumentNotificationsAsync(int documentId, int tenantId)
        {
            try
            {
                var notifications = await _context.ESignatureNotifications
                    .Where(n => n.DocumentId == documentId && n.TenantId == tenantId && !n.IsDeleted)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document notifications for document {DocumentId} tenant {TenantId}", documentId, tenantId);
                throw;
            }
        }

        public async Task<List<ESignatureNotification>> GetSignerNotificationsAsync(int signerId, int tenantId)
        {
            try
            {
                var notifications = await _context.ESignatureNotifications
                    .Where(n => n.SignerId == signerId && n.TenantId == tenantId && !n.IsDeleted)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signer notifications for signer {SignerId} tenant {TenantId}", signerId, tenantId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetNotificationMetricsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.ESignatureNotifications
                    .Where(n => n.TenantId == tenantId && !n.IsDeleted);

                if (fromDate.HasValue)
                    query = query.Where(n => n.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(n => n.CreatedAt <= toDate.Value);

                var notifications = await query.ToListAsync();

                var metrics = new Dictionary<string, object>
                {
                    ["TotalNotifications"] = notifications.Count,
                    ["SentNotifications"] = notifications.Count(n => n.Status == "Sent"),
                    ["PendingNotifications"] = notifications.Count(n => n.Status == "Pending"),
                    ["FailedNotifications"] = notifications.Count(n => n.Status == "Failed"),
                    ["EmailsSent"] = notifications.Count(n => n.EmailSent == true),
                    ["SmsSent"] = notifications.Count(n => n.SmsSent == true),
                    ["NotificationsByType"] = notifications.GroupBy(n => n.NotificationType).ToDictionary(g => g.Key, g => g.Count()),
                    ["NotificationsByChannel"] = notifications.GroupBy(n => n.Channel).ToDictionary(g => g.Key, g => g.Count()),
                    ["DeliveryRate"] = notifications.Count > 0 ? (decimal)notifications.Count(n => n.Status == "Sent") / notifications.Count * 100 : 0,
                    ["EmailDeliveryRate"] = notifications.Count(n => n.Channel == "Email") > 0 ? 
                        (decimal)notifications.Count(n => n.EmailSent == true) / notifications.Count(n => n.Channel == "Email") * 100 : 0,
                    ["SmsDeliveryRate"] = notifications.Count(n => n.Channel == "SMS") > 0 ? 
                        (decimal)notifications.Count(n => n.SmsSent == true) / notifications.Count(n => n.Channel == "SMS") * 100 : 0
                };

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification metrics for tenant {TenantId}", tenantId);
                throw;
            }
        }

        private string GenerateDeclineEmailHtml(ESignatureDocument document, ESignatureSigner signer, string reason)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #e74c3c;'>Document Declined</h2>
                        <p>Your document <strong>{document.Title}</strong> has been declined by {signer.FullName}.</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #e74c3c; margin: 20px 0;'>
                            <strong>Reason:</strong> {reason}
                        </div>
                        <p>You may need to review the document and make necessary changes before resending.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666;'>
                            This email was sent by Nexora E-Signature System.
                        </p>
                    </div>
                </body>
                </html>";
        }

        private string GenerateDelegationEmailHtml(ESignatureDocument document, ESignatureSigner fromSigner, ESignatureSigner toSigner, string reason, string signingLink)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #3498db;'>Document Delegated to You</h2>
                        <p>Dear {toSigner.FullName},</p>
                        <p>The document <strong>{document.Title}</strong> has been delegated to you by {fromSigner.FullName}.</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #3498db; margin: 20px 0;'>
                            <strong>Delegation Reason:</strong> {reason}
                        </div>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{signingLink}' style='background-color: #3498db; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                                Sign Document
                            </a>
                        </div>
                        <p style='font-size: 12px; color: #666;'>
                            Please review and sign the document at your earliest convenience.
                        </p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666;'>
                            This email was sent by Nexora E-Signature System.
                        </p>
                    </div>
                </body>
                </html>";
        }

        private async Task ProcessScheduledNotification(ESignatureNotification notification, int tenantId)
        {
            switch (notification.NotificationType)
            {
                case "Reminder":
                    if (notification.SignerId.HasValue)
                    {
                        if (notification.DocumentId.HasValue)
                            await SendReminderNotificationAsync(notification.DocumentId.Value, notification.SignerId.Value, tenantId);
                    }
                    break;
                case "SigningInvitation":
                    if (notification.SignerId.HasValue)
                    {
                        if (notification.DocumentId.HasValue)
                            await SendSigningInvitationAsync(notification.DocumentId.Value, notification.SignerId.Value, notification.Message, tenantId);
                    }
                    break;
                default:
                    _logger.LogWarning("Unknown scheduled notification type: {NotificationType}", notification.NotificationType);
                    break;
            }

            notification.Status = "Sent";
            notification.SentAt = DateTime.UtcNow;
        }
    }

    public interface IESignatureNotificationService
    {
        Task SendSigningInvitationAsync(int documentId, int signerId, string customMessage, int tenantId);
        Task SendReminderNotificationAsync(int documentId, int signerId, int tenantId);
        Task SendDocumentCompletedNotificationAsync(int documentId, int tenantId);
        Task SendDocumentExpiredNotificationAsync(int documentId, int tenantId);
        Task SendDocumentDeclinedNotificationAsync(int documentId, int signerId, string reason, int tenantId);
        Task SendDocumentDelegatedNotificationAsync(int documentId, int fromSignerId, int toSignerId, string reason, int tenantId);
        Task ProcessScheduledNotificationsAsync(int tenantId);
        Task<List<ESignatureNotification>> GetDocumentNotificationsAsync(int documentId, int tenantId);
        Task<List<ESignatureNotification>> GetSignerNotificationsAsync(int signerId, int tenantId);
        Task<Dictionary<string, object>> GetNotificationMetricsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
