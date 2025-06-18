using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface ISmsService
    {
        Task<SmsResponse> SendSmsAsync(SendSmsRequest request);
        Task<BulkSmsResponse> SendBulkSmsAsync(SendBulkSmsRequest request);
        Task<SmsDeliveryStatusResponse> GetDeliveryStatusAsync(string messageId);
        Task<SmsListResponse> GetMessagesAsync(SmsListRequest request);
        Task<bool> CancelScheduledMessageAsync(string messageId);
        Task<SmsResponse> ResendMessageAsync(string messageId);
        Task ProcessDeliveryReceiptAsync(string providerMessageId, string status, DateTime? deliveredAt = null, string errorCode = null, string errorMessage = null);
        Task<List<SmsProviderDto>> GetProvidersAsync();
        Task<SmsProviderDto> GetProviderAsync(int providerId);
        Task<SmsProviderDto> CreateProviderAsync(CreateSmsProviderRequest request);
        Task<SmsProviderDto> UpdateProviderAsync(int providerId, UpdateSmsProviderRequest request);
        Task<bool> DeleteProviderAsync(int providerId);
        Task<bool> TestProviderConnectionAsync(int providerId);
        Task<Dictionary<string, object>> GetProviderHealthAsync(int providerId);
        Task<decimal> EstimateMessageCostAsync(string message, string messageType, string senderId = null, int? providerId = null);
        Task<int> EstimateMessageSegmentsAsync(string message);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        Task<string> FormatPhoneNumberAsync(string phoneNumber, string countryCode = "SA");
        Task<List<string>> GetSupportedCountriesAsync();
        Task<Dictionary<string, decimal>> GetProviderRatesAsync(int providerId);
        Task UpdateProviderRatesAsync(int providerId, Dictionary<string, decimal> rates);
        Task<bool> IsProviderAvailableAsync(int providerId);
        Task<SmsProviderDto> GetOptimalProviderAsync(string messageType, string country = "SA", decimal? maxCost = null);
        Task<List<SmsProviderDto>> GetProvidersByCountryAsync(string country);
        Task<Dictionary<string, object>> GetProviderStatisticsAsync(int providerId, DateTime fromDate, DateTime toDate);
        Task<bool> UpdateProviderHealthStatusAsync(int providerId, string status, string message = null);
        Task<List<string>> GetProviderSupportedFeaturesAsync(int providerId);
        Task<bool> ValidateProviderConfigurationAsync(int providerId);
        Task<string> GenerateProviderApiKeyAsync(int providerId);
        Task<bool> RotateProviderCredentialsAsync(int providerId);
        Task<Dictionary<string, object>> GetProviderUsageStatisticsAsync(int providerId, string period = "daily");
    }
}
