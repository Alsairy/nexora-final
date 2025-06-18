using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Nexora.Core.Entities;
using Nexora.Core.DTOs;
using Nexora.Core.Interfaces;
using Nexora.Core.Data;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Nexora.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly NexoraDbContext _context;
        private readonly ILogger<SmsService> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IKsaComplianceService _complianceService;

        public SmsService(
            NexoraDbContext context,
            ILogger<SmsService> logger,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IKsaComplianceService complianceService)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _complianceService = complianceService;
        }

        public async Task<SmsResponse> SendSmsAsync(SendSmsRequest request)
        {
            try
            {
                _logger.LogInformation("Sending SMS to {PhoneNumber}", request.ToNumber);

                var tenantId = _currentUserService.TenantId;
                var userId = _currentUserService.UserId;

                var complianceCheck = await _complianceService.CheckComplianceAsync(
                    request.ToNumber, request.Message, request.MessageType, request.SenderId, tenantId);

                if (!complianceCheck.IsCompliant)
                {
                    _logger.LogWarning("SMS compliance check failed for {PhoneNumber}: {Violations}", 
                        request.ToNumber, string.Join(", ", complianceCheck.Violations));
                    
                    return new SmsResponse
                    {
                        Success = false,
                        ErrorMessage = "Message failed compliance check: " + string.Join(", ", complianceCheck.Violations),
                        ErrorCode = "COMPLIANCE_VIOLATION"
                    };
                }

                var provider = await GetOptimalProviderAsync(request.MessageType);
                if (provider == null)
                {
                    return new SmsResponse
                    {
                        Success = false,
                        ErrorMessage = "No available SMS provider found",
                        ErrorCode = "NO_PROVIDER"
                    };
                }

                var cost = await EstimateMessageCostAsync(request.Message, request.MessageType, request.SenderId, provider.Id);

                var smsMessage = new SmsMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    FromNumber = provider.Code,
                    ToNumber = request.ToNumber,
                    MessageContent = request.Message,
                    Status = "Queued",
                    SenderId = request.SenderId,
                    MessageType = request.MessageType,
                    TemplateId = request.TemplateId,
                    Cost = cost,
                    Currency = "SAR",
                    ProviderId = provider.Code,
                    ScheduledAt = request.ScheduledAt,
                    CampaignId = request.CampaignId,
                    Priority = request.Priority,
                    Metadata = request.Metadata,
                    IsCompliant = complianceCheck.IsCompliant,
                    ComplianceNotes = string.Join("; ", complianceCheck.Warnings),
                    IsDndChecked = complianceCheck.DndChecked,
                    IsTimeWindowChecked = complianceCheck.TimeWindowChecked,
                    IsContentFiltered = complianceCheck.ContentFiltered,
                    CreatedBy = userId
                };

                _context.SmsMessages.Add(smsMessage);
                await _context.SaveChangesAsync();

                await QueueMessageForSendingAsync(smsMessage);

                return new SmsResponse
                {
                    Success = true,
                    MessageId = smsMessage.MessageId,
                    Status = smsMessage.Status,
                    Cost = smsMessage.Cost,
                    Currency = smsMessage.Currency,
                    CreatedAt = smsMessage.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", request.ToNumber);
                return new SmsResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error",
                    ErrorCode = "INTERNAL_ERROR"
                };
            }
        }

        public async Task<BulkSmsResponse> SendBulkSmsAsync(SendBulkSmsRequest request)
        {
            try
            {
                _logger.LogInformation("Sending bulk SMS to {RecipientCount} recipients", request.Recipients.Count);

                var tenantId = _currentUserService.TenantId;
                var userId = _currentUserService.UserId;
                var batchId = Guid.NewGuid().ToString();

                var response = new BulkSmsResponse
                {
                    Success = true,
                    TotalMessages = request.Recipients.Count,
                    BatchId = batchId,
                    CreatedAt = DateTime.UtcNow,
                    Currency = "SAR"
                };

                var provider = await GetOptimalProviderAsync(request.MessageType);
                if (provider == null)
                {
                    response.Success = false;
                    response.FailedMessages = request.Recipients.Count;
                    return response;
                }

                foreach (var recipient in request.Recipients)
                {
                    try
                    {
                        var personalizedMessage = PersonalizeMessage(request.Message, recipient.Variables);
                        
                        var complianceCheck = await _complianceService.CheckComplianceAsync(
                            recipient.PhoneNumber, personalizedMessage, request.MessageType, request.SenderId, tenantId);

                        var cost = await EstimateMessageCostAsync(personalizedMessage, request.MessageType, request.SenderId, provider.Id);

                        var smsMessage = new SmsMessage
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            TenantId = tenantId,
                            FromNumber = provider.Code,
                            ToNumber = recipient.PhoneNumber,
                            MessageContent = personalizedMessage,
                            Status = complianceCheck.IsCompliant ? "Queued" : "Rejected",
                            SenderId = request.SenderId,
                            MessageType = request.MessageType,
                            TemplateId = request.TemplateId,
                            Cost = cost,
                            Currency = "SAR",
                            ProviderId = provider.Code,
                            ScheduledAt = request.ScheduledAt,
                            CampaignId = request.CampaignId,
                            BatchId = batchId,
                            Priority = request.Priority,
                            Metadata = request.Metadata,
                            IsCompliant = complianceCheck.IsCompliant,
                            ComplianceNotes = string.Join("; ", complianceCheck.Violations.Concat(complianceCheck.Warnings)),
                            IsDndChecked = complianceCheck.DndChecked,
                            IsTimeWindowChecked = complianceCheck.TimeWindowChecked,
                            IsContentFiltered = complianceCheck.ContentFiltered,
                            CreatedBy = userId
                        };

                        if (!complianceCheck.IsCompliant)
                        {
                            smsMessage.ErrorMessage = string.Join(", ", complianceCheck.Violations);
                            smsMessage.ErrorCode = "COMPLIANCE_VIOLATION";
                        }

                        _context.SmsMessages.Add(smsMessage);

                        var messageResponse = new SmsResponse
                        {
                            Success = complianceCheck.IsCompliant,
                            MessageId = smsMessage.MessageId,
                            Status = smsMessage.Status,
                            Cost = smsMessage.Cost,
                            Currency = smsMessage.Currency,
                            ErrorMessage = smsMessage.ErrorMessage,
                            ErrorCode = smsMessage.ErrorCode,
                            CreatedAt = smsMessage.CreatedAt
                        };

                        response.Messages.Add(messageResponse);
                        response.TotalCost += cost;

                        if (complianceCheck.IsCompliant)
                        {
                            response.SuccessfulMessages++;
                            await QueueMessageForSendingAsync(smsMessage);
                        }
                        else
                        {
                            response.FailedMessages++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing bulk SMS recipient {PhoneNumber}", recipient.PhoneNumber);
                        response.FailedMessages++;
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = response.SuccessfulMessages > 0;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk SMS");
                return new BulkSmsResponse
                {
                    Success = false,
                    TotalMessages = request.Recipients.Count,
                    FailedMessages = request.Recipients.Count
                };
            }
        }

        public async Task<SmsDeliveryStatusResponse> GetDeliveryStatusAsync(string messageId)
        {
            var message = await _context.SmsMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.TenantId == _currentUserService.TenantId);

            if (message == null)
            {
                return null;
            }

            return new SmsDeliveryStatusResponse
            {
                MessageId = message.MessageId,
                Status = message.Status,
                SentAt = message.SentAt,
                DeliveredAt = message.DeliveredAt,
                FailedAt = message.FailedAt,
                ErrorMessage = message.ErrorMessage,
                ErrorCode = message.ErrorCode,
                Cost = message.Cost,
                Currency = message.Currency,
                ProviderId = message.ProviderId,
                ProviderMessageId = message.ProviderMessageId,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt
            };
        }

        public async Task<SmsListResponse> GetMessagesAsync(SmsListRequest request)
        {
            var query = _context.SmsMessages
                .Where(m => m.TenantId == _currentUserService.TenantId);

            if (!string.IsNullOrEmpty(request.Status))
                query = query.Where(m => m.Status == request.Status);

            if (!string.IsNullOrEmpty(request.MessageType))
                query = query.Where(m => m.MessageType == request.MessageType);

            if (!string.IsNullOrEmpty(request.SenderId))
                query = query.Where(m => m.SenderId == request.SenderId);

            if (!string.IsNullOrEmpty(request.CampaignId))
                query = query.Where(m => m.CampaignId == request.CampaignId);

            if (request.FromDate.HasValue)
                query = query.Where(m => m.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(m => m.CreatedAt <= request.ToDate.Value);

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(m => 
                    m.ToNumber.Contains(request.SearchTerm) ||
                    m.MessageContent.Contains(request.SearchTerm) ||
                    m.MessageId.Contains(request.SearchTerm));
            }

            var totalCount = await query.CountAsync();

            if (request.SortOrder.ToUpper() == "DESC")
            {
                query = request.SortBy.ToLower() switch
                {
                    "createdat" => query.OrderByDescending(m => m.CreatedAt),
                    "sentat" => query.OrderByDescending(m => m.SentAt),
                    "status" => query.OrderByDescending(m => m.Status),
                    "cost" => query.OrderByDescending(m => m.Cost),
                    _ => query.OrderByDescending(m => m.CreatedAt)
                };
            }
            else
            {
                query = request.SortBy.ToLower() switch
                {
                    "createdat" => query.OrderBy(m => m.CreatedAt),
                    "sentat" => query.OrderBy(m => m.SentAt),
                    "status" => query.OrderBy(m => m.Status),
                    "cost" => query.OrderBy(m => m.Cost),
                    _ => query.OrderBy(m => m.CreatedAt)
                };
            }

            var messages = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var messageDtos = _mapper.Map<List<SmsMessageDto>>(messages);

            return new SmsListResponse
            {
                Messages = messageDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasNextPage = request.Page * request.PageSize < totalCount,
                HasPreviousPage = request.Page > 1
            };
        }

        public async Task<bool> CancelScheduledMessageAsync(string messageId)
        {
            var message = await _context.SmsMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && 
                                        m.TenantId == _currentUserService.TenantId &&
                                        m.Status == "Queued" &&
                                        m.ScheduledAt.HasValue &&
                                        m.ScheduledAt > DateTime.UtcNow);

            if (message == null)
                return false;

            message.Status = "Cancelled";
            message.UpdatedAt = DateTime.UtcNow;
            message.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SmsResponse> ResendMessageAsync(string messageId)
        {
            var originalMessage = await _context.SmsMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.TenantId == _currentUserService.TenantId);

            if (originalMessage == null)
            {
                return new SmsResponse
                {
                    Success = false,
                    ErrorMessage = "Original message not found",
                    ErrorCode = "MESSAGE_NOT_FOUND"
                };
            }

            var request = new SendSmsRequest
            {
                ToNumber = originalMessage.ToNumber,
                Message = originalMessage.MessageContent,
                SenderId = originalMessage.SenderId,
                MessageType = originalMessage.MessageType,
                TemplateId = originalMessage.TemplateId,
                CampaignId = originalMessage.CampaignId,
                Priority = originalMessage.Priority,
                Metadata = originalMessage.Metadata
            };

            return await SendSmsAsync(request);
        }

        public async Task ProcessDeliveryReceiptAsync(string providerMessageId, string status, DateTime? deliveredAt = null, string errorCode = null, string errorMessage = null)
        {
            var message = await _context.SmsMessages
                .FirstOrDefaultAsync(m => m.ProviderMessageId == providerMessageId);

            if (message == null)
            {
                _logger.LogWarning("Delivery receipt received for unknown message: {ProviderMessageId}", providerMessageId);
                return;
            }

            message.Status = status;
            message.UpdatedAt = DateTime.UtcNow;

            switch (status.ToLower())
            {
                case "delivered":
                    message.DeliveredAt = deliveredAt ?? DateTime.UtcNow;
                    break;
                case "failed":
                case "rejected":
                case "expired":
                    message.FailedAt = DateTime.UtcNow;
                    message.ErrorCode = errorCode;
                    message.ErrorMessage = errorMessage;
                    break;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated delivery status for message {MessageId} to {Status}", message.MessageId, status);
        }

        public async Task<List<SmsProviderDto>> GetProvidersAsync()
        {
            var providers = await _context.SmsProviders
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ToListAsync();

            return _mapper.Map<List<SmsProviderDto>>(providers);
        }

        public async Task<SmsProviderDto> GetProviderAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            return _mapper.Map<SmsProviderDto>(provider);
        }

        public async Task<SmsProviderDto> CreateProviderAsync(CreateSmsProviderRequest request)
        {
            var provider = _mapper.Map<SmsProvider>(request);
            provider.CreatedBy = _currentUserService.UserId;

            _context.SmsProviders.Add(provider);
            await _context.SaveChangesAsync();

            return _mapper.Map<SmsProviderDto>(provider);
        }

        public async Task<SmsProviderDto> UpdateProviderAsync(int providerId, UpdateSmsProviderRequest request)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return null;

            _mapper.Map(request, provider);
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();

            return _mapper.Map<SmsProviderDto>(provider);
        }

        public async Task<bool> DeleteProviderAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return false;

            provider.IsActive = false;
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TestProviderConnectionAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return false;

            try
            {
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider connection test failed for {ProviderId}", providerId);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetProviderHealthAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return null;

            return new Dictionary<string, object>
            {
                ["providerId"] = provider.Id,
                ["name"] = provider.Name,
                ["status"] = provider.HealthStatus,
                ["lastCheck"] = provider.LastHealthCheck,
                ["successRate"] = provider.SuccessRate,
                ["averageDeliveryTime"] = provider.AverageDeliveryTime,
                ["failureCount"] = provider.FailureCount,
                ["lastFailure"] = provider.LastFailureAt
            };
        }

        public async Task<decimal> EstimateMessageCostAsync(string message, string messageType, string senderId = null, int? providerId = null)
        {
            var provider = providerId.HasValue 
                ? await _context.SmsProviders.FirstOrDefaultAsync(p => p.Id == providerId.Value)
                : await GetOptimalProviderEntityAsync(messageType);

            if (provider == null)
                return 0;

            var segments = await EstimateMessageSegmentsAsync(message);
            return provider.CostPerSms * segments;
        }

        public async Task<int> EstimateMessageSegmentsAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                return 0;

            var hasUnicode = message.Any(c => c > 127);
            var maxLength = hasUnicode ? 70 : 160;
            
            return (int)Math.Ceiling((double)message.Length / maxLength);
        }

        public async Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            var regex = new Regex(@"^\+?[1-9]\d{1,14}$");
            return regex.IsMatch(phoneNumber);
        }

        public async Task<string> FormatPhoneNumberAsync(string phoneNumber, string countryCode = "SA")
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            phoneNumber = phoneNumber.Trim().Replace(" ", "").Replace("-", "");

            if (countryCode == "SA" && phoneNumber.StartsWith("05"))
            {
                return "+966" + phoneNumber.Substring(1);
            }

            if (!phoneNumber.StartsWith("+"))
            {
                phoneNumber = "+" + phoneNumber;
            }

            return phoneNumber;
        }

        public async Task<List<string>> GetSupportedCountriesAsync()
        {
            var countries = await _context.SmsProviders
                .Where(p => p.IsActive)
                .Select(p => p.Country)
                .Distinct()
                .ToListAsync();

            return countries;
        }

        public async Task<Dictionary<string, decimal>> GetProviderRatesAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return new Dictionary<string, decimal>();

            return new Dictionary<string, decimal>
            {
                ["sms"] = provider.CostPerSms,
                ["currency"] = 1
            };
        }

        public async Task UpdateProviderRatesAsync(int providerId, Dictionary<string, decimal> rates)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return;

            if (rates.ContainsKey("sms"))
            {
                provider.CostPerSms = rates["sms"];
            }

            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsProviderAvailableAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            return provider != null && provider.IsActive && provider.HealthStatus == "Healthy";
        }

        public async Task<SmsProviderDto> GetOptimalProviderAsync(string messageType, string country = "SA", decimal? maxCost = null)
        {
            var provider = await GetOptimalProviderEntityAsync(messageType, country, maxCost);
            return _mapper.Map<SmsProviderDto>(provider);
        }

        public async Task<List<SmsProviderDto>> GetProvidersByCountryAsync(string country)
        {
            var providers = await _context.SmsProviders
                .Where(p => p.IsActive && p.Country == country)
                .OrderBy(p => p.Priority)
                .ToListAsync();

            return _mapper.Map<List<SmsProviderDto>>(providers);
        }

        public async Task<Dictionary<string, object>> GetProviderStatisticsAsync(int providerId, DateTime fromDate, DateTime toDate)
        {
            var stats = await _context.SmsMessages
                .Where(m => m.ProviderId == providerId.ToString() && 
                           m.CreatedAt >= fromDate && 
                           m.CreatedAt <= toDate)
                .GroupBy(m => 1)
                .Select(g => new
                {
                    TotalMessages = g.Count(),
                    DeliveredMessages = g.Count(m => m.Status == "Delivered"),
                    FailedMessages = g.Count(m => m.Status == "Failed"),
                    TotalCost = g.Sum(m => m.Cost),
                    AverageDeliveryTime = g.Where(m => m.SentAt.HasValue && m.DeliveredAt.HasValue)
                                          .Average(m => (m.DeliveredAt.Value - m.SentAt.Value).TotalSeconds)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                return new Dictionary<string, object>
                {
                    ["totalMessages"] = 0,
                    ["deliveredMessages"] = 0,
                    ["failedMessages"] = 0,
                    ["totalCost"] = 0,
                    ["averageDeliveryTime"] = 0,
                    ["deliveryRate"] = 0
                };
            }

            return new Dictionary<string, object>
            {
                ["totalMessages"] = stats.TotalMessages,
                ["deliveredMessages"] = stats.DeliveredMessages,
                ["failedMessages"] = stats.FailedMessages,
                ["totalCost"] = stats.TotalCost,
                ["averageDeliveryTime"] = stats.AverageDeliveryTime ?? 0,
                ["deliveryRate"] = stats.TotalMessages > 0 ? (decimal)stats.DeliveredMessages / stats.TotalMessages * 100 : 0
            };
        }

        public async Task<bool> UpdateProviderHealthStatusAsync(int providerId, string status, string message = null)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return false;

            provider.HealthStatus = status;
            provider.HealthStatusMessage = message;
            provider.LastHealthCheck = DateTime.UtcNow;
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetProviderSupportedFeaturesAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return new List<string>();

            var features = new List<string>();

            if (provider.SupportsDeliveryReceipts)
                features.Add("DeliveryReceipts");

            if (provider.SupportsUnicode)
                features.Add("Unicode");

            if (provider.SupportsLongMessages)
                features.Add("LongMessages");

            if (provider.IsKsaCompliant)
                features.Add("KSACompliance");

            if (provider.SupportsCitcIntegration)
                features.Add("CITCIntegration");

            return features;
        }

        public async Task<bool> ValidateProviderConfigurationAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return false;

            var isValid = !string.IsNullOrEmpty(provider.ApiEndpoint) &&
                         !string.IsNullOrEmpty(provider.ApiKey) &&
                         provider.CostPerSms > 0;

            return isValid;
        }

        public async Task<string> GenerateProviderApiKeyAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return null;

            var apiKey = Guid.NewGuid().ToString("N");
            provider.ApiKey = apiKey;
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return apiKey;
        }

        public async Task<bool> RotateProviderCredentialsAsync(int providerId)
        {
            var provider = await _context.SmsProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return false;

            provider.ApiKey = Guid.NewGuid().ToString("N");
            provider.ApiSecret = Guid.NewGuid().ToString("N");
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, object>> GetProviderUsageStatisticsAsync(int providerId, string period = "daily")
        {
            var fromDate = period.ToLower() switch
            {
                "hourly" => DateTime.UtcNow.AddHours(-24),
                "daily" => DateTime.UtcNow.AddDays(-30),
                "weekly" => DateTime.UtcNow.AddDays(-7 * 12),
                "monthly" => DateTime.UtcNow.AddMonths(-12),
                _ => DateTime.UtcNow.AddDays(-30)
            };

            var usage = await _context.SmsMessages
                .Where(m => m.ProviderId == providerId.ToString() && m.CreatedAt >= fromDate)
                .GroupBy(m => m.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    MessageCount = g.Count(),
                    Cost = g.Sum(m => m.Cost)
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["period"] = period,
                ["fromDate"] = fromDate,
                ["toDate"] = DateTime.UtcNow,
                ["usage"] = usage
            };
        }

        private async Task<SmsProvider> GetOptimalProviderEntityAsync(string messageType, string country = "SA", decimal? maxCost = null)
        {
            var query = _context.SmsProviders
                .Where(p => p.IsActive && p.Country == country && p.HealthStatus == "Healthy");

            if (maxCost.HasValue)
                query = query.Where(p => p.CostPerSms <= maxCost.Value);

            return await query
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.CostPerSms)
                .FirstOrDefaultAsync();
        }

        private async Task QueueMessageForSendingAsync(SmsMessage message)
        {
            _logger.LogInformation("Queuing message {MessageId} for sending", message.MessageId);
        }

        private string PersonalizeMessage(string template, Dictionary<string, string> variables)
        {
            if (variables == null || !variables.Any())
                return template;

            var result = template;
            foreach (var variable in variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value);
            }

            return result;
        }
    }
}
