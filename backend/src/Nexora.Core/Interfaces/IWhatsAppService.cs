using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface IWhatsAppService
    {
        Task<WhatsAppResponse> SendMessageAsync(SendWhatsAppRequest request);
        Task<WhatsAppResponse> SendTemplateMessageAsync(SendWhatsAppTemplateRequest request);
        Task<BulkWhatsAppResponse> SendBulkMessageAsync(SendBulkWhatsAppRequest request);
        Task<WhatsAppDeliveryStatusResponse> GetDeliveryStatusAsync(string messageId);
        Task<List<WhatsAppTemplate>> GetApprovedTemplatesAsync(int tenantId);
        Task<WhatsAppTemplate> CreateTemplateAsync(CreateWhatsAppTemplateRequest request);
        Task<bool> DeleteTemplateAsync(string templateId, int tenantId);
        Task<WhatsAppBusinessProfile> GetBusinessProfileAsync(int tenantId);
        Task<WhatsAppBusinessProfile> UpdateBusinessProfileAsync(UpdateWhatsAppBusinessProfileRequest request);
        Task<List<WhatsAppContact>> GetContactsAsync(int tenantId);
        Task<WhatsAppContact> AddContactAsync(AddWhatsAppContactRequest request);
        Task<bool> RemoveContactAsync(string contactId, int tenantId);
        Task<List<WhatsAppConversation>> GetConversationsAsync(int tenantId, int page = 1, int pageSize = 50);
        Task<WhatsAppConversation> GetConversationAsync(string conversationId, int tenantId);
        Task<WhatsAppResponse> SendInteractiveMessageAsync(SendWhatsAppInteractiveRequest request);
        Task<WhatsAppResponse> SendMediaMessageAsync(SendWhatsAppMediaRequest request);
        Task<bool> MarkMessageAsReadAsync(string messageId, int tenantId);
        Task<WhatsAppWebhookResponse> ProcessWebhookAsync(WhatsAppWebhookRequest request);
        Task<List<WhatsAppMessage>> GetMessageHistoryAsync(string phoneNumber, int tenantId, int page = 1, int pageSize = 50);
        Task<WhatsAppAnalytics> GetAnalyticsAsync(int tenantId, DateTime fromDate, DateTime toDate);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        Task<WhatsAppBusinessVerification> GetBusinessVerificationStatusAsync(int tenantId);
        Task<bool> RequestBusinessVerificationAsync(WhatsAppBusinessVerificationRequest request);
    }

    public class SendWhatsAppRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public string? PreviewUrl { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }

    public class SendWhatsAppTemplateRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "en";
        public List<WhatsAppTemplateParameter> Parameters { get; set; } = new List<WhatsAppTemplateParameter>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
    }

    public class SendBulkWhatsAppRequest
    {
        public int TenantId { get; set; }
        public List<WhatsAppRecipient> Recipients { get; set; } = new List<WhatsAppRecipient>();
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public string? TemplateName { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }

    public class SendWhatsAppInteractiveRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string InteractiveType { get; set; } = string.Empty; // button, list, flow
        public WhatsAppInteractiveContent Content { get; set; } = new WhatsAppInteractiveContent();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
    }

    public class SendWhatsAppMediaRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty; // image, video, audio, document
        public string MediaUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string? Filename { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
    }

    public class WhatsAppResponse
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

    public class BulkWhatsAppResponse
    {
        public int TotalMessages { get; set; }
        public int SuccessfulMessages { get; set; }
        public int FailedMessages { get; set; }
        public List<WhatsAppResponse> Results { get; set; } = new List<WhatsAppResponse>();
        public decimal TotalCost { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class WhatsAppDeliveryStatusResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal Cost { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<WhatsAppTemplateComponent> Components { get; set; } = new List<WhatsAppTemplateComponent>();
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class WhatsAppTemplateComponent
    {
        public string Type { get; set; } = string.Empty; // HEADER, BODY, FOOTER, BUTTONS
        public string? Text { get; set; }
        public List<WhatsAppTemplateParameter> Parameters { get; set; } = new List<WhatsAppTemplateParameter>();
        public List<WhatsAppButton> Buttons { get; set; } = new List<WhatsAppButton>();
    }

    public class WhatsAppTemplateParameter
    {
        public string Type { get; set; } = string.Empty; // text, currency, date_time, image, video, document
        public string Value { get; set; } = string.Empty;
    }

    public class WhatsAppButton
    {
        public string Type { get; set; } = string.Empty; // QUICK_REPLY, URL, PHONE_NUMBER
        public string Text { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class WhatsAppRecipient
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public List<WhatsAppTemplateParameter> Parameters { get; set; } = new List<WhatsAppTemplateParameter>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppInteractiveContent
    {
        public WhatsAppInteractiveHeader? Header { get; set; }
        public WhatsAppInteractiveBody Body { get; set; } = new WhatsAppInteractiveBody();
        public WhatsAppInteractiveFooter? Footer { get; set; }
        public WhatsAppInteractiveAction Action { get; set; } = new WhatsAppInteractiveAction();
    }

    public class WhatsAppInteractiveHeader
    {
        public string Type { get; set; } = string.Empty; // text, image, video, document
        public string Text { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
    }

    public class WhatsAppInteractiveBody
    {
        public string Text { get; set; } = string.Empty;
    }

    public class WhatsAppInteractiveFooter
    {
        public string Text { get; set; } = string.Empty;
    }

    public class WhatsAppInteractiveAction
    {
        public List<WhatsAppButton> Buttons { get; set; } = new List<WhatsAppButton>();
        public List<WhatsAppListSection> Sections { get; set; } = new List<WhatsAppListSection>();
    }

    public class WhatsAppListSection
    {
        public string Title { get; set; } = string.Empty;
        public List<WhatsAppListRow> Rows { get; set; } = new List<WhatsAppListRow>();
    }

    public class WhatsAppListRow
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class WhatsAppBusinessProfile
    {
        public string BusinessId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class WhatsAppContact
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastSeenAt { get; set; }
        public bool IsBlocked { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppConversation
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public List<WhatsAppMessage> Messages { get; set; } = new List<WhatsAppMessage>();
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppMessage
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsInbound { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppAnalytics
    {
        public int TenantId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalMessages { get; set; }
        public int DeliveredMessages { get; set; }
        public int ReadMessages { get; set; }
        public int FailedMessages { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal ReadRate { get; set; }
        public decimal TotalCost { get; set; }
        public List<WhatsAppMessageTypeStats> MessageTypeStats { get; set; } = new List<WhatsAppMessageTypeStats>();
        public List<WhatsAppTemplateStats> TemplateStats { get; set; } = new List<WhatsAppTemplateStats>();
        public Dictionary<string, int> MessagesByHour { get; set; } = new Dictionary<string, int>();
    }

    public class WhatsAppMessageTypeStats
    {
        public string MessageType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class WhatsAppTemplateStats
    {
        public string TemplateName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class WhatsAppBusinessVerification
    {
        public string BusinessId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<string> RequiredDocuments { get; set; } = new List<string>();
        public List<string> SubmittedDocuments { get; set; } = new List<string>();
    }

    public class CreateWhatsAppTemplateRequest
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<WhatsAppTemplateComponent> Components { get; set; } = new List<WhatsAppTemplateComponent>();
    }

    public class UpdateWhatsAppBusinessProfileRequest
    {
        public int TenantId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class AddWhatsAppContactRequest
    {
        public int TenantId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppWebhookRequest
    {
        public string Event { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    public class WhatsAppWebhookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class WhatsAppBusinessVerificationRequest
    {
        public int TenantId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string BusinessWebsite { get; set; } = string.Empty;
        public List<string> DocumentUrls { get; set; } = new List<string>();
    }
}
