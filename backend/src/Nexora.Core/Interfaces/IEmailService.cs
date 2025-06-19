using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface IEmailService
    {
        Task<EmailResponse> SendEmailAsync(SendEmailRequest request);
        Task<EmailResponse> SendTemplateEmailAsync(SendEmailTemplateRequest request);
        Task<BulkEmailResponse> SendBulkEmailAsync(SendBulkEmailRequest request);
        Task<EmailDeliveryStatusResponse> GetDeliveryStatusAsync(string messageId);
        Task<List<EmailTemplate>> GetTemplatesAsync(int tenantId);
        Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request);
        Task<EmailTemplate> UpdateTemplateAsync(UpdateEmailTemplateRequest request);
        Task<bool> DeleteTemplateAsync(string templateId, int tenantId);
        Task<List<EmailContact>> GetContactsAsync(int tenantId);
        Task<EmailContact> AddContactAsync(AddEmailContactRequest request);
        Task<bool> RemoveContactAsync(string contactId, int tenantId);
        Task<List<EmailList>> GetEmailListsAsync(int tenantId);
        Task<EmailList> CreateEmailListAsync(CreateEmailListRequest request);
        Task<bool> AddContactToListAsync(string listId, string contactId, int tenantId);
        Task<bool> RemoveContactFromListAsync(string listId, string contactId, int tenantId);
        Task<EmailAnalytics> GetAnalyticsAsync(int tenantId, DateTime fromDate, DateTime toDate);
        Task<List<EmailEvent>> GetEmailEventsAsync(string messageId, int tenantId);
        Task<EmailWebhookResponse> ProcessWebhookAsync(EmailWebhookRequest request);
        Task<bool> ValidateEmailAddressAsync(string emailAddress);
        Task<EmailDomainVerification> GetDomainVerificationAsync(string domain, int tenantId);
        Task<EmailDomainVerification> VerifyDomainAsync(VerifyEmailDomainRequest request);
        Task<List<EmailSuppression>> GetSuppressionListAsync(int tenantId);
        Task<bool> AddToSuppressionListAsync(AddEmailSuppressionRequest request);
        Task<bool> RemoveFromSuppressionListAsync(string emailAddress, int tenantId);
        Task<EmailBounceResponse> ProcessBounceAsync(EmailBounceRequest request);
        Task<EmailComplaintResponse> ProcessComplaintAsync(EmailComplaintRequest request);
    }

    public class SendEmailRequest
    {
        public int TenantId { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public List<string> ToEmails { get; set; } = new List<string>();
        public List<string> CcEmails { get; set; } = new List<string>();
        public List<string> BccEmails { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string Priority { get; set; } = "normal"; // low, normal, high
        public bool TrackOpens { get; set; } = true;
        public bool TrackClicks { get; set; } = true;
    }

    public class SendEmailTemplateRequest
    {
        public int TenantId { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public List<EmailRecipient> Recipients { get; set; } = new List<EmailRecipient>();
        public string TemplateId { get; set; } = string.Empty;
        public Dictionary<string, object> TemplateData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public bool TrackOpens { get; set; } = true;
        public bool TrackClicks { get; set; } = true;
    }

    public class SendBulkEmailRequest
    {
        public int TenantId { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public List<EmailRecipient> Recipients { get; set; } = new List<EmailRecipient>();
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string? TemplateId { get; set; }
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public bool TrackOpens { get; set; } = true;
        public bool TrackClicks { get; set; } = true;
    }

    public class EmailResponse
    {
        public bool Success { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal Cost { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class BulkEmailResponse
    {
        public int TotalEmails { get; set; }
        public int SuccessfulEmails { get; set; }
        public int FailedEmails { get; set; }
        public List<EmailResponse> Results { get; set; } = new List<EmailResponse>();
        public decimal TotalCost { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class EmailDeliveryStatusResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClickedAt { get; set; }
        public DateTime? BouncedAt { get; set; }
        public DateTime? ComplainedAt { get; set; }
        public string? BounceReason { get; set; }
        public string? ComplaintReason { get; set; }
        public int OpenCount { get; set; }
        public int ClickCount { get; set; }
        public List<string> ClickedUrls { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class EmailTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Variables { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? PreviewUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class EmailContact
    {
        public string Id { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Lists { get; set; } = new List<string>();
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastEmailSentAt { get; set; }
        public bool IsUnsubscribed { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
    }

    public class EmailList
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ContactCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class EmailRecipient
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string? Name { get; set; }
        public Dictionary<string, object> TemplateData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string? ContentId { get; set; }
        public bool IsInline { get; set; } = false;
    }

    public class EmailAnalytics
    {
        public int TenantId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalEmails { get; set; }
        public int DeliveredEmails { get; set; }
        public int BouncedEmails { get; set; }
        public int OpenedEmails { get; set; }
        public int ClickedEmails { get; set; }
        public int UnsubscribedEmails { get; set; }
        public int ComplainedEmails { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal BounceRate { get; set; }
        public decimal ComplaintRate { get; set; }
        public decimal UnsubscribeRate { get; set; }
        public decimal TotalCost { get; set; }
        public List<EmailCampaignStats> CampaignStats { get; set; } = new List<EmailCampaignStats>();
        public List<EmailTemplateStats> TemplateStats { get; set; } = new List<EmailTemplateStats>();
        public Dictionary<string, int> EmailsByHour { get; set; } = new Dictionary<string, int>();
        public List<EmailDeviceStats> DeviceStats { get; set; } = new List<EmailDeviceStats>();
        public List<EmailLocationStats> LocationStats { get; set; } = new List<EmailLocationStats>();
    }

    public class EmailCampaignStats
    {
        public string CampaignId { get; set; } = string.Empty;
        public string CampaignName { get; set; } = string.Empty;
        public int EmailsSent { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class EmailTemplateStats
    {
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class EmailDeviceStats
    {
        public string DeviceType { get; set; } = string.Empty;
        public int OpenCount { get; set; }
        public int ClickCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class EmailLocationStats
    {
        public string Country { get; set; } = string.Empty;
        public string? City { get; set; }
        public int OpenCount { get; set; }
        public int ClickCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class EmailEvent
    {
        public string Id { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public string? Url { get; set; }
        public Dictionary<string, object> EventData { get; set; } = new Dictionary<string, object>();
    }

    public class EmailDomainVerification
    {
        public string Domain { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<EmailDnsRecord> DnsRecords { get; set; } = new List<EmailDnsRecord>();
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class EmailDnsRecord
    {
        public string Type { get; set; } = string.Empty; // TXT, CNAME, MX
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }

    public class EmailSuppression
    {
        public string Id { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty; // bounce, complaint, unsubscribe, manual
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
    }

    public class CreateEmailTemplateRequest
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> Variables { get; set; } = new List<string>();
    }

    public class UpdateEmailTemplateRequest
    {
        public int TenantId { get; set; }
        public string TemplateId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> Variables { get; set; } = new List<string>();
    }

    public class AddEmailContactRequest
    {
        public int TenantId { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Lists { get; set; } = new List<string>();
    }

    public class CreateEmailListRequest
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class VerifyEmailDomainRequest
    {
        public int TenantId { get; set; }
        public string Domain { get; set; } = string.Empty;
    }

    public class AddEmailSuppressionRequest
    {
        public int TenantId { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class EmailWebhookRequest
    {
        public string Event { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    public class EmailWebhookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EmailBounceRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string BounceType { get; set; } = string.Empty; // hard, soft
        public string BounceSubType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class EmailBounceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool AddedToSuppression { get; set; }
    }

    public class EmailComplaintRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ComplaintType { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class EmailComplaintResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool AddedToSuppression { get; set; }
    }
}
