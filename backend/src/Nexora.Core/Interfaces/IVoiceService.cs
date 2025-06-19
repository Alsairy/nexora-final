using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface IVoiceService
    {
        Task<VoiceCallResponse> MakeCallAsync(MakeVoiceCallRequest request);
        Task<VoiceCallResponse> PlayMessageAsync(PlayVoiceMessageRequest request);
        Task<VoiceCallResponse> PlayTtsAsync(PlayTtsRequest request);
        Task<VoiceCallResponse> RecordCallAsync(RecordVoiceCallRequest request);
        Task<VoiceCallResponse> TransferCallAsync(TransferCallRequest request);
        Task<VoiceCallResponse> HangupCallAsync(string callId, int tenantId);
        Task<VoiceCallStatus> GetCallStatusAsync(string callId, int tenantId);
        Task<List<VoiceCall>> GetCallHistoryAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50);
        Task<VoiceRecording> GetRecordingAsync(string recordingId, int tenantId);
        Task<List<VoiceRecording>> GetRecordingsAsync(int tenantId, int page = 1, int pageSize = 50);
        Task<bool> DeleteRecordingAsync(string recordingId, int tenantId);
        Task<VoiceAnalytics> GetAnalyticsAsync(int tenantId, DateTime fromDate, DateTime toDate);
        Task<List<VoiceNumber>> GetAvailableNumbersAsync(string countryCode, string? areaCode = null);
        Task<VoiceNumber> PurchaseNumberAsync(PurchaseVoiceNumberRequest request);
        Task<bool> ReleaseNumberAsync(string numberId, int tenantId);
        Task<List<VoiceNumber>> GetOwnedNumbersAsync(int tenantId);
        Task<VoiceWebhookResponse> ProcessWebhookAsync(VoiceWebhookRequest request);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        Task<VoiceConferenceResponse> CreateConferenceAsync(CreateVoiceConferenceRequest request);
        Task<VoiceConferenceResponse> JoinConferenceAsync(JoinVoiceConferenceRequest request);
        Task<bool> LeaveConferenceAsync(string conferenceId, string participantId, int tenantId);
        Task<VoiceConference> GetConferenceAsync(string conferenceId, int tenantId);
        Task<List<VoiceConference>> GetConferencesAsync(int tenantId, int page = 1, int pageSize = 50);
    }

    public class MakeVoiceCallRequest
    {
        public int TenantId { get; set; }
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? TtsText { get; set; }
        public string? TtsVoice { get; set; } = "en-US-Standard-A";
        public string? AudioUrl { get; set; }
        public bool RecordCall { get; set; } = false;
        public int? MaxDuration { get; set; } = 300; // seconds
        public string? CallbackUrl { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public DateTime? ScheduledTime { get; set; }
    }

    public class PlayVoiceMessageRequest
    {
        public int TenantId { get; set; }
        public string CallId { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public bool Loop { get; set; } = false;
        public int? MaxPlays { get; set; } = 1;
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class PlayTtsRequest
    {
        public int TenantId { get; set; }
        public string CallId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Voice { get; set; } = "en-US-Standard-A";
        public string Language { get; set; } = "en-US";
        public decimal Speed { get; set; } = 1.0m;
        public decimal Pitch { get; set; } = 0.0m;
        public bool Loop { get; set; } = false;
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class RecordVoiceCallRequest
    {
        public int TenantId { get; set; }
        public string CallId { get; set; } = string.Empty;
        public int? MaxDuration { get; set; } = 60; // seconds
        public string? RecordingFormat { get; set; } = "mp3";
        public bool TranscribeRecording { get; set; } = false;
        public string? TranscriptionLanguage { get; set; } = "en-US";
        public string? CallbackUrl { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class TransferCallRequest
    {
        public int TenantId { get; set; }
        public string CallId { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public string TransferType { get; set; } = "blind"; // blind, attended
        public string? TransferMessage { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceCallResponse
    {
        public bool Success { get; set; }
        public string CallId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal Cost { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceCallStatus
    {
        public string CallId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // inbound, outbound
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; } // seconds
        public decimal Cost { get; set; }
        public string? RecordingUrl { get; set; }
        public string? TranscriptionText { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceCall
    {
        public string Id { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; }
        public decimal Cost { get; set; }
        public string? RecordingId { get; set; }
        public string? TranscriptionText { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceRecording
    {
        public string Id { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string RecordingUrl { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public int Duration { get; set; } // seconds
        public long FileSize { get; set; } // bytes
        public string? TranscriptionText { get; set; }
        public decimal? TranscriptionConfidence { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceAnalytics
    {
        public int TenantId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalCalls { get; set; }
        public int AnsweredCalls { get; set; }
        public int MissedCalls { get; set; }
        public int FailedCalls { get; set; }
        public decimal AnswerRate { get; set; }
        public decimal AverageCallDuration { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCost { get; set; }
        public List<VoiceCallTypeStats> CallTypeStats { get; set; } = new List<VoiceCallTypeStats>();
        public Dictionary<string, int> CallsByHour { get; set; } = new Dictionary<string, int>();
        public List<VoiceNumberStats> NumberStats { get; set; } = new List<VoiceNumberStats>();
    }

    public class VoiceCallTypeStats
    {
        public string CallType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal AnswerRate { get; set; }
        public decimal AverageDuration { get; set; }
        public decimal Cost { get; set; }
    }

    public class VoiceNumberStats
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public int InboundCalls { get; set; }
        public int OutboundCalls { get; set; }
        public decimal AnswerRate { get; set; }
        public decimal Cost { get; set; }
    }

    public class VoiceNumber
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string? AreaCode { get; set; }
        public string NumberType { get; set; } = string.Empty; // local, toll-free, mobile
        public string Status { get; set; } = string.Empty;
        public decimal MonthlyCost { get; set; }
        public decimal PerMinuteCost { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>(); // voice, sms, mms
        public DateTime? PurchasedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class PurchaseVoiceNumberRequest
    {
        public int TenantId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? FriendlyName { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceWebhookRequest
    {
        public string Event { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int? Duration { get; set; }
        public decimal? Cost { get; set; }
        public string? RecordingUrl { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceWebhookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CreateVoiceConferenceRequest
    {
        public int TenantId { get; set; }
        public string ConferenceName { get; set; } = string.Empty;
        public int MaxParticipants { get; set; } = 10;
        public bool RecordConference { get; set; } = false;
        public bool RequirePin { get; set; } = false;
        public string? Pin { get; set; }
        public string? WelcomeMessage { get; set; }
        public string? CallbackUrl { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class JoinVoiceConferenceRequest
    {
        public int TenantId { get; set; }
        public string ConferenceId { get; set; } = string.Empty;
        public string ParticipantNumber { get; set; } = string.Empty;
        public string? ParticipantName { get; set; }
        public string? Pin { get; set; }
        public bool Muted { get; set; } = false;
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceConferenceResponse
    {
        public bool Success { get; set; }
        public string ConferenceId { get; set; } = string.Empty;
        public string? ParticipantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceConference
    {
        public string Id { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public bool RecordConference { get; set; }
        public string? RecordingUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int? Duration { get; set; }
        public List<VoiceConferenceParticipant> Participants { get; set; } = new List<VoiceConferenceParticipant>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class VoiceConferenceParticipant
    {
        public string Id { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public int? Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
