using FluentValidation;
using System;
using System.Text.RegularExpressions;

namespace Nexora.Core.DTOs.Validators
{
    public class SendSmsRequestValidator : AbstractValidator<SendSmsRequest>
    {
        public SendSmsRequestValidator()
        {
            RuleFor(x => x.ToNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message content is required")
                .MaximumLength(1600).WithMessage("Message cannot exceed 1600 characters");

            RuleFor(x => x.SenderId)
                .MaximumLength(20).WithMessage("Sender ID cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.SenderId));

            RuleFor(x => x.MessageType)
                .NotEmpty().WithMessage("Message type is required")
                .Must(BeValidMessageType).WithMessage("Invalid message type. Must be Transactional, Promotional, or OTP");

            RuleFor(x => x.ScheduledAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled time must be in the future")
                .When(x => x.ScheduledAt.HasValue);

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10");

            RuleFor(x => x.CampaignId)
                .MaximumLength(100).WithMessage("Campaign ID cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.CampaignId));

            RuleFor(x => x.Metadata)
                .MaximumLength(500).WithMessage("Metadata cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Metadata));
        }

        private bool BeValidMessageType(string messageType)
        {
            var validTypes = new[] { "Transactional", "Promotional", "OTP", "Alert", "Marketing" };
            return Array.Exists(validTypes, type => type.Equals(messageType, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class SendBulkSmsRequestValidator : AbstractValidator<SendBulkSmsRequest>
    {
        public SendBulkSmsRequestValidator()
        {
            RuleFor(x => x.Recipients)
                .NotEmpty().WithMessage("Recipients list cannot be empty")
                .Must(x => x.Count <= 10000).WithMessage("Cannot send to more than 10,000 recipients at once");

            RuleForEach(x => x.Recipients).SetValidator(new BulkSmsRecipientValidator());

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message content is required")
                .MaximumLength(1600).WithMessage("Message cannot exceed 1600 characters");

            RuleFor(x => x.SenderId)
                .MaximumLength(20).WithMessage("Sender ID cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.SenderId));

            RuleFor(x => x.MessageType)
                .NotEmpty().WithMessage("Message type is required")
                .Must(BeValidMessageType).WithMessage("Invalid message type");

            RuleFor(x => x.ScheduledAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled time must be in the future")
                .When(x => x.ScheduledAt.HasValue);

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10");
        }

        private bool BeValidMessageType(string messageType)
        {
            var validTypes = new[] { "Transactional", "Promotional", "OTP", "Alert", "Marketing" };
            return Array.Exists(validTypes, type => type.Equals(messageType, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class BulkSmsRecipientValidator : AbstractValidator<BulkSmsRecipient>
    {
        public BulkSmsRecipientValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

            RuleFor(x => x.Variables)
                .Must(x => x.Count <= 50).WithMessage("Cannot have more than 50 variables per recipient")
                .When(x => x.Variables != null);
        }
    }

    public class CreateSmsTemplateRequestValidator : AbstractValidator<CreateSmsTemplateRequest>
    {
        public CreateSmsTemplateRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(100).WithMessage("Template name cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Template name can only contain letters, numbers, spaces, hyphens, and underscores");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Template content is required")
                .MaximumLength(1600).WithMessage("Template content cannot exceed 1600 characters");

            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("Category cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));

            RuleFor(x => x.MessageType)
                .NotEmpty().WithMessage("Message type is required")
                .Must(BeValidMessageType).WithMessage("Invalid message type");

            RuleFor(x => x.Language)
                .MaximumLength(20).WithMessage("Language code cannot exceed 20 characters")
                .Must(BeValidLanguageCode).WithMessage("Invalid language code")
                .When(x => !string.IsNullOrEmpty(x.Language));

            RuleFor(x => x.Variables)
                .MaximumLength(500).WithMessage("Variables cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Variables));

            RuleFor(x => x.Tags)
                .MaximumLength(500).WithMessage("Tags cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Tags));

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }

        private bool BeValidMessageType(string messageType)
        {
            var validTypes = new[] { "Transactional", "Promotional", "OTP", "Alert", "Marketing" };
            return Array.Exists(validTypes, type => type.Equals(messageType, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidLanguageCode(string languageCode)
        {
            var validCodes = new[] { "en", "ar", "en-US", "ar-SA" };
            return Array.Exists(validCodes, code => code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class CreateSmsProviderRequestValidator : AbstractValidator<CreateSmsProviderRequest>
    {
        public CreateSmsProviderRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Provider name is required")
                .MaximumLength(100).WithMessage("Provider name cannot exceed 100 characters");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Provider code is required")
                .MaximumLength(100).WithMessage("Provider code cannot exceed 100 characters")
                .Matches(@"^[A-Z0-9_]+$").WithMessage("Provider code can only contain uppercase letters, numbers, and underscores");

            RuleFor(x => x.ApiEndpoint)
                .NotEmpty().WithMessage("API endpoint is required")
                .MaximumLength(200).WithMessage("API endpoint cannot exceed 200 characters")
                .Must(BeValidUrl).WithMessage("Invalid API endpoint URL");

            RuleFor(x => x.AuthType)
                .Must(BeValidAuthType).WithMessage("Invalid authentication type")
                .When(x => !string.IsNullOrEmpty(x.AuthType));

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(50).WithMessage("Country cannot exceed 50 characters");

            RuleFor(x => x.CostPerSms)
                .GreaterThan(0).WithMessage("Cost per SMS must be greater than 0");

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10");

            RuleFor(x => x.MaxConcurrentMessages)
                .GreaterThan(0).WithMessage("Max concurrent messages must be greater than 0")
                .LessThanOrEqualTo(10000).WithMessage("Max concurrent messages cannot exceed 10,000");

            RuleFor(x => x.RateLimitPerSecond)
                .GreaterThan(0).WithMessage("Rate limit per second must be greater than 0")
                .LessThanOrEqualTo(1000).WithMessage("Rate limit per second cannot exceed 1,000");

            RuleFor(x => x.CitcProviderId)
                .MaximumLength(100).WithMessage("CITC Provider ID cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.CitcProviderId));

            RuleFor(x => x.ConfigurationJson)
                .MaximumLength(500).WithMessage("Configuration JSON cannot exceed 500 characters")
                .Must(BeValidJson).WithMessage("Invalid JSON format")
                .When(x => !string.IsNullOrEmpty(x.ConfigurationJson));
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        private bool BeValidAuthType(string authType)
        {
            var validTypes = new[] { "ApiKey", "Basic", "Bearer", "OAuth2", "Custom" };
            return Array.Exists(validTypes, type => type.Equals(authType, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidJson(string json)
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class SmsAnalyticsRequestValidator : AbstractValidator<SmsAnalyticsRequest>
    {
        public SmsAnalyticsRequestValidator()
        {
            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(x => x.ToDate).WithMessage("From date must be before or equal to To date")
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

            RuleFor(x => x.ToDate)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("To date cannot be in the future")
                .When(x => x.ToDate.HasValue);

            RuleFor(x => x.Period)
                .Must(BeValidPeriod).WithMessage("Invalid period. Must be Daily, Weekly, or Monthly")
                .When(x => !string.IsNullOrEmpty(x.Period));

            RuleFor(x => x.GroupBy)
                .Must(BeValidGroupBy).WithMessage("Invalid GroupBy value")
                .When(x => !string.IsNullOrEmpty(x.GroupBy));

            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 1000).WithMessage("Page size must be between 1 and 1000");
        }

        private bool BeValidPeriod(string period)
        {
            var validPeriods = new[] { "Daily", "Weekly", "Monthly", "Hourly" };
            return Array.Exists(validPeriods, p => p.Equals(period, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidGroupBy(string groupBy)
        {
            var validGroupBy = new[] { "Date", "Campaign", "SenderId", "MessageType", "Provider", "Network", "Country" };
            return Array.Exists(validGroupBy, g => g.Equals(groupBy, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class SmsReportRequestValidator : AbstractValidator<SmsReportRequest>
    {
        public SmsReportRequestValidator()
        {
            RuleFor(x => x.FromDate)
                .NotEmpty().WithMessage("From date is required")
                .LessThanOrEqualTo(x => x.ToDate).WithMessage("From date must be before or equal to To date");

            RuleFor(x => x.ToDate)
                .NotEmpty().WithMessage("To date is required")
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("To date cannot be in the future");

            RuleFor(x => x.ReportType)
                .NotEmpty().WithMessage("Report type is required")
                .Must(BeValidReportType).WithMessage("Invalid report type. Must be Summary, Detailed, Compliance, or Financial");

            RuleFor(x => x.Format)
                .Must(BeValidFormat).WithMessage("Invalid format. Must be JSON, CSV, PDF, or Excel")
                .When(x => !string.IsNullOrEmpty(x.Format));

            RuleFor(x => x.GroupBy)
                .Must(BeValidGroupBy).WithMessage("Invalid GroupBy value")
                .When(x => !string.IsNullOrEmpty(x.GroupBy));

            RuleFor(x => x.TimeZone)
                .Must(BeValidTimeZone).WithMessage("Invalid time zone")
                .When(x => !string.IsNullOrEmpty(x.TimeZone));
        }

        private bool BeValidReportType(string reportType)
        {
            var validTypes = new[] { "Summary", "Detailed", "Compliance", "Financial", "Performance" };
            return Array.Exists(validTypes, type => type.Equals(reportType, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidFormat(string format)
        {
            var validFormats = new[] { "JSON", "CSV", "PDF", "Excel" };
            return Array.Exists(validFormats, f => f.Equals(format, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidGroupBy(string groupBy)
        {
            var validGroupBy = new[] { "Date", "Campaign", "SenderId", "MessageType", "Provider", "Network", "Country" };
            return Array.Exists(validGroupBy, g => g.Equals(groupBy, StringComparison.OrdinalIgnoreCase));
        }

        private bool BeValidTimeZone(string timeZone)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
