using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface IRcsService
    {
        Task<RcsResponse> SendMessageAsync(SendRcsRequest request);
        Task<RcsResponse> SendRichCardAsync(SendRcsRichCardRequest request);
        Task<RcsResponse> SendCarouselAsync(SendRcsCarouselRequest request);
        Task<BulkRcsResponse> SendBulkMessageAsync(SendBulkRcsRequest request);
        Task<RcsDeliveryStatusResponse> GetDeliveryStatusAsync(string messageId);
        Task<List<RcsTemplate>> GetTemplatesAsync(int tenantId);
        Task<RcsTemplate> CreateTemplateAsync(CreateRcsTemplateRequest request);
        Task<bool> DeleteTemplateAsync(string templateId, int tenantId);
        Task<RcsCapabilityResponse> CheckCapabilityAsync(string phoneNumber);
        Task<List<RcsContact>> GetContactsAsync(int tenantId);
        Task<RcsContact> AddContactAsync(AddRcsContactRequest request);
        Task<bool> RemoveContactAsync(string contactId, int tenantId);
        Task<RcsAnalytics> GetAnalyticsAsync(int tenantId, DateTime fromDate, DateTime toDate);
        Task<List<RcsConversation>> GetConversationsAsync(int tenantId, int page = 1, int pageSize = 50);
        Task<RcsConversation> GetConversationAsync(string conversationId, int tenantId);
        Task<RcsWebhookResponse> ProcessWebhookAsync(RcsWebhookRequest request);
        Task<List<RcsMessage>> GetMessageHistoryAsync(string phoneNumber, int tenantId, int page = 1, int pageSize = 50);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        Task<RcsBrandingResponse> GetBrandingAsync(int tenantId);
        Task<RcsBrandingResponse> UpdateBrandingAsync(UpdateRcsBrandingRequest request);
        Task<List<RcsAgent>> GetAgentsAsync(int tenantId);
        Task<RcsAgent> CreateAgentAsync(CreateRcsAgentRequest request);
        Task<bool> DeleteAgentAsync(string agentId, int tenantId);
        Task<RcsVerificationResponse> GetVerificationStatusAsync(int tenantId);
        Task<bool> RequestVerificationAsync(RcsVerificationRequest request);
    }

    public class SendRcsRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public RcsMediaContent? MediaContent { get; set; }
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? AgentId { get; set; }
    }

    public class SendRcsRichCardRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public RcsRichCard RichCard { get; set; } = new RcsRichCard();
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? AgentId { get; set; }
    }

    public class SendRcsCarouselRequest
    {
        public int TenantId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public List<RcsRichCard> Cards { get; set; } = new List<RcsRichCard>();
        public string CardWidth { get; set; } = "MEDIUM"; // SMALL, MEDIUM
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? AgentId { get; set; }
    }

    public class SendBulkRcsRequest
    {
        public int TenantId { get; set; }
        public List<RcsRecipient> Recipients { get; set; } = new List<RcsRecipient>();
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public string? TemplateId { get; set; }
        public RcsMediaContent? MediaContent { get; set; }
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? AgentId { get; set; }
    }

    public class RcsResponse
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

    public class BulkRcsResponse
    {
        public int TotalMessages { get; set; }
        public int SuccessfulMessages { get; set; }
        public int FailedMessages { get; set; }
        public List<RcsResponse> Results { get; set; } = new List<RcsResponse>();
        public decimal TotalCost { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class RcsDeliveryStatusResponse
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

    public class RcsTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public RcsMediaContent? MediaContent { get; set; }
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public int UsageCount { get; set; }
    }

    public class RcsMediaContent
    {
        public string MediaType { get; set; } = string.Empty; // image, video, audio, file
        public string MediaUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? Caption { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; } // for video/audio in seconds
    }

    public class RcsSuggestedAction
    {
        public string Type { get; set; } = string.Empty; // reply, url, dial, location, calendar
        public string Text { get; set; } = string.Empty;
        public string? PostbackData { get; set; }
        public string? Url { get; set; }
        public string? PhoneNumber { get; set; }
        public RcsLocation? Location { get; set; }
        public RcsCalendarEvent? CalendarEvent { get; set; }
    }

    public class RcsLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Label { get; set; }
        public string? PlaceId { get; set; }
    }

    public class RcsCalendarEvent
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
    }

    public class RcsRichCard
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RcsMediaContent? MediaContent { get; set; }
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
    }

    public class RcsRecipient
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public Dictionary<string, object> TemplateData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class RcsCapabilityResponse
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsRcsCapable { get; set; }
        public List<string> SupportedFeatures { get; set; } = new List<string>();
        public string? CarrierName { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    public class RcsContact
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool IsRcsCapable { get; set; }
        public List<string> SupportedFeatures { get; set; } = new List<string>();
        public string? CarrierName { get; set; }
        public DateTime LastCapabilityCheck { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RcsAnalytics
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
        public List<RcsMessageTypeStats> MessageTypeStats { get; set; } = new List<RcsMessageTypeStats>();
        public List<RcsTemplateStats> TemplateStats { get; set; } = new List<RcsTemplateStats>();
        public Dictionary<string, int> MessagesByHour { get; set; } = new Dictionary<string, int>();
        public List<RcsCarrierStats> CarrierStats { get; set; } = new List<RcsCarrierStats>();
        public List<RcsActionStats> ActionStats { get; set; } = new List<RcsActionStats>();
    }

    public class RcsMessageTypeStats
    {
        public string MessageType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal ReadRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class RcsTemplateStats
    {
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal ReadRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class RcsCarrierStats
    {
        public string CarrierName { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal ReadRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class RcsActionStats
    {
        public string ActionType { get; set; } = string.Empty;
        public int ClickCount { get; set; }
        public decimal ClickRate { get; set; }
    }

    public class RcsConversation
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public List<RcsMessage> Messages { get; set; } = new List<RcsMessage>();
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AgentId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RcsMessage
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public RcsMediaContent? MediaContent { get; set; }
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsInbound { get; set; }
        public string? AgentId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RcsBrandingResponse
    {
        public string BrandId { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? BrandColor { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class RcsAgent
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class RcsVerificationResponse
    {
        public string BrandId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<string> RequiredDocuments { get; set; } = new List<string>();
        public List<string> SubmittedDocuments { get; set; } = new List<string>();
    }

    public class CreateRcsTemplateRequest
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public RcsMediaContent? MediaContent { get; set; }
        public List<RcsSuggestedAction> SuggestedActions { get; set; } = new List<RcsSuggestedAction>();
    }

    public class AddRcsContactRequest
    {
        public int TenantId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class UpdateRcsBrandingRequest
    {
        public int TenantId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? BrandColor { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
    }

    public class CreateRcsAgentRequest
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    public class RcsVerificationRequest
    {
        public int TenantId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string BrandType { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public List<string> DocumentUrls { get; set; } = new List<string>();
    }

    public class RcsWebhookRequest
    {
        public string Event { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    public class RcsWebhookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
