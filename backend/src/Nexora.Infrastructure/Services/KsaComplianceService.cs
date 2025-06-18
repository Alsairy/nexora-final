using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nexora.Core.Interfaces;
using Nexora.Core.Data;
using Nexora.Core.Entities;

namespace Nexora.Infrastructure.Services
{
    public class KsaComplianceService : IKsaComplianceService
    {
        private readonly NexoraDbContext _context;
        private readonly ILogger<KsaComplianceService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUserService;

        private readonly List<string> _prohibitedKeywords = new List<string>
        {
            "gambling", "casino", "bet", "lottery", "alcohol", "wine", "beer", "vodka",
            "adult", "porn", "sex", "dating", "escort", "massage", "cannabis", "drugs",
            "loan shark", "quick money", "get rich", "pyramid", "mlm", "investment guaranteed"
        };

        private readonly Dictionary<string, TimeWindow> _messageTypeTimeWindows = new Dictionary<string, TimeWindow>
        {
            ["Promotional"] = new TimeWindow
            {
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(21, 0, 0),
                AllowedDays = new List<DayOfWeek> { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday },
                IsBusinessHoursOnly = true
            },
            ["Marketing"] = new TimeWindow
            {
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(21, 0, 0),
                AllowedDays = new List<DayOfWeek> { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday },
                IsBusinessHoursOnly = true
            },
            ["Transactional"] = new TimeWindow
            {
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(23, 59, 59),
                AllowedDays = Enum.GetValues<DayOfWeek>().ToList(),
                IsBusinessHoursOnly = false
            },
            ["OTP"] = new TimeWindow
            {
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(23, 59, 59),
                AllowedDays = Enum.GetValues<DayOfWeek>().ToList(),
                IsBusinessHoursOnly = false
            },
            ["Alert"] = new TimeWindow
            {
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(23, 59, 59),
                AllowedDays = Enum.GetValues<DayOfWeek>().ToList(),
                IsBusinessHoursOnly = false
            }
        };

        public KsaComplianceService(
            NexoraDbContext context,
            ILogger<KsaComplianceService> logger,
            IConfiguration configuration,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _currentUserService = currentUserService;
        }

        public async Task<ComplianceCheckResult> CheckComplianceAsync(string phoneNumber, string message, string messageType, string senderId, int tenantId)
        {
            var result = new ComplianceCheckResult
            {
                IsCompliant = true,
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                var isDnd = await IsDndNumberAsync(phoneNumber);
                result.DndChecked = true;
                if (isDnd)
                {
                    result.IsCompliant = false;
                    result.Violations.Add("Phone number is in Do Not Disturb (DND) list");
                }

                var isOptedOut = await IsOptedOutAsync(phoneNumber, tenantId);
                if (isOptedOut)
                {
                    result.IsCompliant = false;
                    result.Violations.Add("Recipient has opted out from receiving messages");
                }

                var isTimeAllowed = await IsTimeWindowAllowedAsync(messageType);
                result.TimeWindowChecked = true;
                if (!isTimeAllowed)
                {
                    result.IsCompliant = false;
                    result.Violations.Add($"Message type '{messageType}' is not allowed at current time");
                }

                var isContentCompliant = await IsContentCompliantAsync(message, messageType);
                result.ContentFiltered = true;
                if (!isContentCompliant)
                {
                    result.IsCompliant = false;
                    result.Violations.Add("Message content violates compliance rules");
                }

                if (!string.IsNullOrEmpty(senderId))
                {
                    var isSenderIdApproved = await IsSenderIdApprovedAsync(senderId, tenantId);
                    result.SenderIdValidated = true;
                    if (!isSenderIdApproved)
                    {
                        result.IsCompliant = false;
                        result.Violations.Add($"Sender ID '{senderId}' is not approved for use");
                    }
                }

                var isUrlCompliant = await CheckUrlComplianceAsync(message);
                if (!isUrlCompliant)
                {
                    result.IsCompliant = false;
                    result.Violations.Add("Message contains non-compliant URLs");
                }

                var isRateLimitCompliant = await CheckRateLimitComplianceAsync(senderId, tenantId);
                if (!isRateLimitCompliant)
                {
                    result.Warnings.Add("Rate limit threshold approaching for this sender ID");
                }

                var totalChecks = 6;
                var passedChecks = totalChecks - result.Violations.Count;
                result.ComplianceScore = $"{(passedChecks * 100 / totalChecks)}%";

                if (result.Violations.Any())
                {
                    foreach (var violation in result.Violations)
                    {
                        await LogComplianceViolationAsync(phoneNumber, violation, messageType, tenantId);
                    }
                }

                _logger.LogInformation("Compliance check completed for {PhoneNumber}: {IsCompliant}", phoneNumber, result.IsCompliant);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during compliance check for {PhoneNumber}", phoneNumber);
                result.IsCompliant = false;
                result.Violations.Add("Compliance check failed due to system error");
                return result;
            }
        }

        public async Task<bool> IsDndNumberAsync(string phoneNumber)
        {
            var normalizedNumber = await NormalizePhoneNumberAsync(phoneNumber);
            
            var dndRecord = await _context.SmsCompliances
                .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedNumber && c.IsDnd);

            return dndRecord != null;
        }

        public async Task<bool> IsTimeWindowAllowedAsync(string messageType, DateTime? scheduledTime = null)
        {
            var checkTime = scheduledTime ?? DateTime.UtcNow;
            var riyadhTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(checkTime, "Asia/Riyadh");

            if (!_messageTypeTimeWindows.ContainsKey(messageType))
            {
                _logger.LogWarning("Unknown message type for time window check: {MessageType}", messageType);
                return true; // Allow unknown types by default
            }

            var timeWindow = _messageTypeTimeWindows[messageType];

            if (!timeWindow.AllowedDays.Contains(riyadhTime.DayOfWeek))
            {
                return false;
            }

            var currentTime = riyadhTime.TimeOfDay;
            if (currentTime < timeWindow.StartTime || currentTime > timeWindow.EndTime)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> IsContentCompliantAsync(string message, string messageType)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            var lowerMessage = message.ToLower();

            foreach (var keyword in _prohibitedKeywords)
            {
                if (lowerMessage.Contains(keyword.ToLower()))
                {
                    _logger.LogWarning("Prohibited keyword '{Keyword}' found in message", keyword);
                    return false;
                }
            }

            var maxLength = messageType switch
            {
                "OTP" => 160,
                "Alert" => 160,
                "Transactional" => 1600,
                "Promotional" => 1600,
                "Marketing" => 1600,
                _ => 1600
            };

            if (message.Length > maxLength)
            {
                _logger.LogWarning("Message exceeds maximum length for type {MessageType}: {Length}/{MaxLength}", 
                    messageType, message.Length, maxLength);
                return false;
            }

            if (HasSuspiciousPatterns(message))
            {
                return false;
            }

            return true;
        }

        public async Task<bool> IsSenderIdApprovedAsync(string senderId, int tenantId)
        {
            var senderIdRecord = await _context.SenderIds
                .FirstOrDefaultAsync(s => s.SenderId == senderId && 
                                         s.TenantId == tenantId && 
                                         s.Status == "Approved" && 
                                         s.IsActive &&
                                         (!s.ExpiresAt.HasValue || s.ExpiresAt > DateTime.UtcNow));

            return senderIdRecord != null;
        }

        public async Task<List<string>> GetProhibitedKeywordsAsync()
        {
            return _prohibitedKeywords.ToList();
        }

        public async Task<bool> ValidateMessageContentAsync(string message, string messageType)
        {
            return await IsContentCompliantAsync(message, messageType);
        }

        public async Task<bool> CheckUrlComplianceAsync(string message)
        {
            var urlPattern = @"https?://[^\s]+";
            var urls = Regex.Matches(message, urlPattern, RegexOptions.IgnoreCase);

            foreach (Match url in urls)
            {
                var urlString = url.Value.ToLower();
                
                var blacklistedDomains = new[] { "bit.ly", "tinyurl.com", "t.co", "goo.gl" };
                if (blacklistedDomains.Any(domain => urlString.Contains(domain)))
                {
                    _logger.LogWarning("Blacklisted URL domain found: {Url}", urlString);
                    return false;
                }

                if (urlString.Contains("phishing") || urlString.Contains("malware") || urlString.Contains("spam"))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<ComplianceReport> GenerateComplianceReportAsync(int tenantId, DateTime fromDate, DateTime toDate)
        {
            var violations = await GetComplianceViolationsAsync(tenantId, fromDate, toDate);
            var totalMessages = await _context.SmsMessages
                .CountAsync(m => m.TenantId == tenantId && m.CreatedAt >= fromDate && m.CreatedAt <= toDate);

            var compliantMessages = await _context.SmsMessages
                .CountAsync(m => m.TenantId == tenantId && 
                               m.CreatedAt >= fromDate && 
                               m.CreatedAt <= toDate && 
                               m.IsCompliant);

            var report = new ComplianceReport
            {
                TenantId = tenantId,
                FromDate = fromDate,
                ToDate = toDate,
                TotalMessages = totalMessages,
                CompliantMessages = compliantMessages,
                ViolationCount = violations.Count,
                Violations = violations,
                ViolationsByType = violations.GroupBy(v => v.ViolationType)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                ComplianceRate = totalMessages > 0 ? (decimal)compliantMessages / totalMessages * 100 : 100,
                GeneratedAt = DateTime.UtcNow
            };

            return report;
        }

        public async Task<bool> RegisterSenderIdAsync(string senderId, int tenantId, string businessName, string businessType)
        {
            var existingSenderId = await _context.SenderIds
                .FirstOrDefaultAsync(s => s.SenderId == senderId && s.TenantId == tenantId);

            if (existingSenderId != null)
            {
                _logger.LogWarning("Sender ID {SenderId} already exists for tenant {TenantId}", senderId, tenantId);
                return false;
            }

            var newSenderId = new SenderId
            {
                SenderId = senderId,
                TenantId = tenantId,
                BusinessName = businessName,
                BusinessType = businessType,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow,
                IsActive = false,
                CreatedBy = _currentUserService.UserId
            };

            _context.SenderIds.Add(newSenderId);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sender ID {SenderId} registered for tenant {TenantId}", senderId, tenantId);
            return true;
        }

        public async Task<SenderIdStatus> GetSenderIdStatusAsync(string senderId, int tenantId)
        {
            var senderIdRecord = await _context.SenderIds
                .FirstOrDefaultAsync(s => s.SenderId == senderId && s.TenantId == tenantId);

            if (senderIdRecord == null)
                return null;

            return new SenderIdStatus
            {
                SenderId = senderIdRecord.SenderId,
                TenantId = senderIdRecord.TenantId,
                Status = senderIdRecord.Status,
                RequestedAt = senderIdRecord.RequestedAt,
                ApprovedAt = senderIdRecord.ApprovedAt,
                RejectedAt = senderIdRecord.RejectedAt,
                RejectionReason = senderIdRecord.RejectionReason,
                BusinessName = senderIdRecord.BusinessName,
                BusinessType = senderIdRecord.BusinessType,
                IsActive = senderIdRecord.IsActive,
                ExpiresAt = senderIdRecord.ExpiresAt
            };
        }

        public async Task<bool> UpdateDndListAsync(List<string> phoneNumbers, bool isDnd)
        {
            foreach (var phoneNumber in phoneNumbers)
            {
                var normalizedNumber = await NormalizePhoneNumberAsync(phoneNumber);
                var existingRecord = await _context.SmsCompliances
                    .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedNumber);

                if (existingRecord != null)
                {
                    existingRecord.IsDnd = isDnd;
                    existingRecord.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newRecord = new SmsCompliance
                    {
                        PhoneNumber = normalizedNumber,
                        IsDnd = isDnd,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SmsCompliances.Add(newRecord);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetDndNumbersAsync(int tenantId)
        {
            var dndNumbers = await _context.SmsCompliances
                .Where(c => c.IsDnd)
                .Select(c => c.PhoneNumber)
                .ToListAsync();

            return dndNumbers;
        }

        public async Task<bool> ValidatePhoneNumberFormatAsync(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            var patterns = new[]
            {
                @"^\+966[5][0-9]{8}$",  // +966 5XXXXXXXX
                @"^966[5][0-9]{8}$",   // 966 5XXXXXXXX
                @"^05[0-9]{8}$"        // 05XXXXXXXX
            };

            return patterns.Any(pattern => Regex.IsMatch(phoneNumber, pattern));
        }

        public async Task<string> NormalizePhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            var normalized = phoneNumber.Trim().Replace(" ", "").Replace("-", "");

            if (normalized.StartsWith("05"))
            {
                normalized = "+966" + normalized.Substring(1);
            }
            else if (normalized.StartsWith("966") && !normalized.StartsWith("+966"))
            {
                normalized = "+" + normalized;
            }
            else if (!normalized.StartsWith("+"))
            {
                normalized = "+" + normalized;
            }

            return normalized;
        }

        public async Task<bool> IsBusinessHoursAsync(DateTime? dateTime = null)
        {
            var checkTime = dateTime ?? DateTime.UtcNow;
            var riyadhTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(checkTime, "Asia/Riyadh");

            var businessDays = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };
            var businessStartTime = new TimeSpan(8, 0, 0);
            var businessEndTime = new TimeSpan(18, 0, 0);

            return businessDays.Contains(riyadhTime.DayOfWeek) &&
                   riyadhTime.TimeOfDay >= businessStartTime &&
                   riyadhTime.TimeOfDay <= businessEndTime;
        }

        public async Task<TimeWindow> GetAllowedTimeWindowAsync(string messageType)
        {
            return _messageTypeTimeWindows.ContainsKey(messageType) 
                ? _messageTypeTimeWindows[messageType] 
                : _messageTypeTimeWindows["Transactional"];
        }

        public async Task<bool> CheckRateLimitComplianceAsync(string senderId, int tenantId)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentMessageCount = await _context.SmsMessages
                .CountAsync(m => m.SenderId == senderId && 
                               m.TenantId == tenantId && 
                               m.CreatedAt >= oneHourAgo);

            var hourlyLimit = 1000; // Default hourly limit
            return recentMessageCount < hourlyLimit;
        }

        public async Task<bool> LogComplianceViolationAsync(string phoneNumber, string violation, string messageType, int tenantId)
        {
            var violationRecord = new ComplianceViolation
            {
                TenantId = tenantId,
                PhoneNumber = phoneNumber,
                ViolationType = GetViolationType(violation),
                Description = violation,
                MessageType = messageType,
                OccurredAt = DateTime.UtcNow,
                Severity = GetViolationSeverity(violation),
                IsResolved = false
            };

            _logger.LogWarning("Compliance violation logged: {Violation} for {PhoneNumber}", violation, phoneNumber);
            return true;
        }

        public async Task<List<ComplianceViolation>> GetComplianceViolationsAsync(int tenantId, DateTime fromDate, DateTime toDate)
        {
            return new List<ComplianceViolation>();
        }

        public async Task<bool> IsOptedOutAsync(string phoneNumber, int tenantId)
        {
            var normalizedNumber = await NormalizePhoneNumberAsync(phoneNumber);
            var optOutRecord = await _context.SmsCompliances
                .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedNumber && c.IsOptedOut);

            return optOutRecord != null;
        }

        public async Task<bool> ProcessOptOutRequestAsync(string phoneNumber, int tenantId)
        {
            var normalizedNumber = await NormalizePhoneNumberAsync(phoneNumber);
            var existingRecord = await _context.SmsCompliances
                .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedNumber);

            if (existingRecord != null)
            {
                existingRecord.IsOptedOut = true;
                existingRecord.OptedOutAt = DateTime.UtcNow;
                existingRecord.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newRecord = new SmsCompliance
                {
                    PhoneNumber = normalizedNumber,
                    IsOptedOut = true,
                    OptedOutAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SmsCompliances.Add(newRecord);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Opt-out processed for {PhoneNumber}", phoneNumber);
            return true;
        }

        public async Task<bool> ProcessOptInRequestAsync(string phoneNumber, int tenantId)
        {
            var normalizedNumber = await NormalizePhoneNumberAsync(phoneNumber);
            var existingRecord = await _context.SmsCompliances
                .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedNumber);

            if (existingRecord != null)
            {
                existingRecord.IsOptedOut = false;
                existingRecord.OptedInAt = DateTime.UtcNow;
                existingRecord.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Opt-in processed for {PhoneNumber}", phoneNumber);
            return true;
        }

        public async Task<CitcIntegrationStatus> GetCitcIntegrationStatusAsync(int tenantId)
        {
            return new CitcIntegrationStatus
            {
                TenantId = tenantId,
                IsIntegrated = false,
                Status = "Not Configured",
                LastSyncAt = null
            };
        }

        public async Task<bool> SyncWithCitcAsync(int tenantId)
        {
            _logger.LogInformation("CITC sync initiated for tenant {TenantId}", tenantId);
            return true;
        }

        public async Task<bool> ValidateMessageTemplateAsync(string templateContent, string messageType)
        {
            return await IsContentCompliantAsync(templateContent, messageType);
        }

        public async Task<List<string>> GetComplianceRecommendationsAsync(string message, string messageType)
        {
            var recommendations = new List<string>();

            if (message.Length > 160 && (messageType == "OTP" || messageType == "Alert"))
            {
                recommendations.Add("Consider shortening the message for OTP/Alert types to stay within 160 characters");
            }

            if (HasSuspiciousPatterns(message))
            {
                recommendations.Add("Message contains patterns that may be flagged as suspicious");
            }

            var urls = Regex.Matches(message, @"https?://[^\s]+", RegexOptions.IgnoreCase);
            if (urls.Count > 0)
            {
                recommendations.Add("Ensure all URLs are from trusted domains and properly formatted");
            }

            return recommendations;
        }

        private bool HasSuspiciousPatterns(string message)
        {
            var suspiciousPatterns = new[]
            {
                @"\b(urgent|immediate|act now|limited time)\b",
                @"\b(free|win|winner|congratulations)\b",
                @"\b(click here|call now|text back)\b",
                @"[A-Z]{5,}", // Too many consecutive capitals
                @"[!]{3,}",   // Too many exclamation marks
                @"[$€£¥₹]{2,}" // Multiple currency symbols
            };

            return suspiciousPatterns.Any(pattern => 
                Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase));
        }

        private string GetViolationType(string violation)
        {
            if (violation.Contains("DND") || violation.Contains("Do Not Disturb"))
                return "DND_VIOLATION";
            if (violation.Contains("time") || violation.Contains("window"))
                return "TIME_WINDOW_VIOLATION";
            if (violation.Contains("content") || violation.Contains("keyword"))
                return "CONTENT_VIOLATION";
            if (violation.Contains("sender") || violation.Contains("ID"))
                return "SENDER_ID_VIOLATION";
            if (violation.Contains("opted out"))
                return "OPT_OUT_VIOLATION";
            if (violation.Contains("URL"))
                return "URL_VIOLATION";

            return "GENERAL_VIOLATION";
        }

        private string GetViolationSeverity(string violation)
        {
            if (violation.Contains("DND") || violation.Contains("opted out"))
                return "HIGH";
            if (violation.Contains("prohibited") || violation.Contains("blacklisted"))
                return "HIGH";
            if (violation.Contains("time window") || violation.Contains("sender ID"))
                return "MEDIUM";

            return "LOW";
        }
    }
}
