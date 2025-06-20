using FluentValidation;
using System;
using System.Linq;

namespace Nexora.Core.DTOs.Validators
{
    public class CreateESignatureDocumentRequestValidator : AbstractValidator<CreateESignatureDocumentRequest>
    {
        public CreateESignatureDocumentRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.DocumentType)
                .NotEmpty().WithMessage("Document type is required")
                .MaximumLength(100).WithMessage("Document type cannot exceed 100 characters");

            RuleFor(x => x.DocumentUrl)
                .NotEmpty().WithMessage("Document URL is required")
                .Must(BeValidUrl).WithMessage("Document URL must be a valid URL");

            RuleFor(x => x.ExpiryDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Expiry date must be in the future")
                .When(x => x.ExpiryDate.HasValue);

            RuleFor(x => x.SigningInstructions)
                .MaximumLength(1000).WithMessage("Signing instructions cannot exceed 1000 characters");

            RuleFor(x => x.AuthenticationMethod)
                .Must(BeValidAuthenticationMethod).WithMessage("Invalid authentication method")
                .When(x => !string.IsNullOrEmpty(x.AuthenticationMethod));

            RuleFor(x => x.Language)
                .Must(BeValidLanguage).WithMessage("Invalid language code")
                .When(x => !string.IsNullOrEmpty(x.Language));

            RuleFor(x => x.CalendarType)
                .Must(BeValidCalendarType).WithMessage("Invalid calendar type")
                .When(x => !string.IsNullOrEmpty(x.CalendarType));

            RuleFor(x => x.Signers)
                .NotEmpty().WithMessage("At least one signer is required")
                .Must(HaveUniqueSigningOrders).WithMessage("Signing orders must be unique")
                .Must(HaveUniqueEmails).WithMessage("Signer emails must be unique");

            RuleForEach(x => x.Signers).SetValidator(new CreateESignatureSignerRequestValidator());
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        private bool BeValidAuthenticationMethod(string method)
        {
            var validMethods = new[] { "Password", "OTP", "Nafath", "Certificate", "Biometric" };
            return validMethods.Contains(method);
        }

        private bool BeValidLanguage(string language)
        {
            var validLanguages = new[] { "en", "ar" };
            return validLanguages.Contains(language);
        }

        private bool BeValidCalendarType(string calendarType)
        {
            var validTypes = new[] { "Gregorian", "Hijri" };
            return validTypes.Contains(calendarType);
        }

        private bool HaveUniqueSigningOrders(System.Collections.Generic.List<CreateESignatureSignerRequest> signers)
        {
            return signers.GroupBy(s => s.SigningOrder).All(g => g.Count() == 1);
        }

        private bool HaveUniqueEmails(System.Collections.Generic.List<CreateESignatureSignerRequest> signers)
        {
            return signers.GroupBy(s => s.Email.ToLower()).All(g => g.Count() == 1);
        }
    }

    public class CreateESignatureSignerRequestValidator : AbstractValidator<CreateESignatureSignerRequest>
    {
        public CreateESignatureSignerRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(200).WithMessage("Email cannot exceed 200 characters");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.NationalId)
                .MaximumLength(20).WithMessage("National ID cannot exceed 20 characters")
                .Matches(@"^[0-9]+$").WithMessage("National ID must contain only numbers")
                .When(x => !string.IsNullOrEmpty(x.NationalId));

            RuleFor(x => x.SignerType)
                .NotEmpty().WithMessage("Signer type is required")
                .Must(BeValidSignerType).WithMessage("Invalid signer type");

            RuleFor(x => x.SigningOrder)
                .GreaterThan(0).WithMessage("Signing order must be greater than 0");

            RuleFor(x => x.AuthenticationMethod)
                .Must(BeValidAuthenticationMethod).WithMessage("Invalid authentication method")
                .When(x => !string.IsNullOrEmpty(x.AuthenticationMethod));

            RuleFor(x => x.SigningInstructions)
                .MaximumLength(1000).WithMessage("Signing instructions cannot exceed 1000 characters");
        }

        private bool BeValidSignerType(string signerType)
        {
            var validTypes = new[] { "Primary", "Secondary", "Witness", "Approver" };
            return validTypes.Contains(signerType);
        }

        private bool BeValidAuthenticationMethod(string method)
        {
            var validMethods = new[] { "Password", "OTP", "Nafath", "Certificate", "Biometric" };
            return validMethods.Contains(method);
        }
    }

    public class CreateSignatureRequestValidator : AbstractValidator<CreateSignatureRequest>
    {
        public CreateSignatureRequestValidator()
        {
            RuleFor(x => x.DocumentId)
                .GreaterThan(0).WithMessage("Document ID must be greater than 0");

            RuleFor(x => x.SignerId)
                .GreaterThan(0).WithMessage("Signer ID must be greater than 0");

            RuleFor(x => x.SignatureType)
                .NotEmpty().WithMessage("Signature type is required")
                .Must(BeValidSignatureType).WithMessage("Invalid signature type");

            RuleFor(x => x.SignatureData)
                .NotEmpty().WithMessage("Signature data is required");

            RuleFor(x => x.AuthenticationMethod)
                .Must(BeValidAuthenticationMethod).WithMessage("Invalid authentication method")
                .When(x => !string.IsNullOrEmpty(x.AuthenticationMethod));

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0")
                .When(x => x.PageNumber.HasValue);

            RuleFor(x => x.PositionX)
                .GreaterThanOrEqualTo(0).WithMessage("Position X must be greater than or equal to 0")
                .When(x => x.PositionX.HasValue);

            RuleFor(x => x.PositionY)
                .GreaterThanOrEqualTo(0).WithMessage("Position Y must be greater than or equal to 0")
                .When(x => x.PositionY.HasValue);

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("Width must be greater than 0")
                .When(x => x.Width.HasValue);

            RuleFor(x => x.Height)
                .GreaterThan(0).WithMessage("Height must be greater than 0")
                .When(x => x.Height.HasValue);

            RuleFor(x => x.SigningReason)
                .MaximumLength(1000).WithMessage("Signing reason cannot exceed 1000 characters");

            RuleFor(x => x.SigningLocation)
                .MaximumLength(200).WithMessage("Signing location cannot exceed 200 characters");
        }

        private bool BeValidSignatureType(string signatureType)
        {
            var validTypes = new[] { "Electronic", "Digital", "Biometric", "Handwritten" };
            return validTypes.Contains(signatureType);
        }

        private bool BeValidAuthenticationMethod(string method)
        {
            var validMethods = new[] { "Password", "OTP", "Nafath", "Certificate", "Biometric" };
            return validMethods.Contains(method);
        }
    }

    public class CreateESignatureWorkflowRequestValidator : AbstractValidator<CreateESignatureWorkflowRequest>
    {
        public CreateESignatureWorkflowRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Workflow name is required")
                .MaximumLength(200).WithMessage("Workflow name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.WorkflowType)
                .NotEmpty().WithMessage("Workflow type is required")
                .Must(BeValidWorkflowType).WithMessage("Invalid workflow type");

            RuleFor(x => x.AuthenticationMethod)
                .Must(BeValidAuthenticationMethod).WithMessage("Invalid authentication method")
                .When(x => !string.IsNullOrEmpty(x.AuthenticationMethod));

            RuleFor(x => x.ExpiryDays)
                .GreaterThan(0).WithMessage("Expiry days must be greater than 0")
                .LessThanOrEqualTo(365).WithMessage("Expiry days cannot exceed 365")
                .When(x => x.ExpiryDays.HasValue);

            RuleFor(x => x.ReminderIntervalHours)
                .GreaterThan(0).WithMessage("Reminder interval must be greater than 0")
                .LessThanOrEqualTo(168).WithMessage("Reminder interval cannot exceed 168 hours (1 week)")
                .When(x => x.ReminderIntervalHours.HasValue);

            RuleFor(x => x.Steps)
                .NotEmpty().WithMessage("At least one workflow step is required")
                .Must(HaveUniqueStepOrders).WithMessage("Step orders must be unique");

            RuleForEach(x => x.Steps).SetValidator(new CreateESignatureWorkflowStepRequestValidator());
        }

        private bool BeValidWorkflowType(string workflowType)
        {
            var validTypes = new[] { "Sequential", "Parallel", "Custom" };
            return validTypes.Contains(workflowType);
        }

        private bool BeValidAuthenticationMethod(string method)
        {
            var validMethods = new[] { "Password", "OTP", "Nafath", "Certificate", "Biometric" };
            return validMethods.Contains(method);
        }

        private bool HaveUniqueStepOrders(System.Collections.Generic.List<CreateESignatureWorkflowStepRequest> steps)
        {
            return steps.GroupBy(s => s.StepOrder).All(g => g.Count() == 1);
        }
    }

    public class CreateESignatureWorkflowStepRequestValidator : AbstractValidator<CreateESignatureWorkflowStepRequest>
    {
        public CreateESignatureWorkflowStepRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Step name is required")
                .MaximumLength(200).WithMessage("Step name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.StepOrder)
                .GreaterThan(0).WithMessage("Step order must be greater than 0");

            RuleFor(x => x.StepType)
                .NotEmpty().WithMessage("Step type is required")
                .Must(BeValidStepType).WithMessage("Invalid step type");

            RuleFor(x => x.AssigneeType)
                .NotEmpty().WithMessage("Assignee type is required")
                .Must(BeValidAssigneeType).WithMessage("Invalid assignee type");

            RuleFor(x => x.AssigneeValue)
                .NotEmpty().WithMessage("Assignee value is required");

            RuleFor(x => x.AuthenticationMethod)
                .Must(BeValidAuthenticationMethod).WithMessage("Invalid authentication method")
                .When(x => !string.IsNullOrEmpty(x.AuthenticationMethod));

            RuleFor(x => x.TimeoutHours)
                .GreaterThan(0).WithMessage("Timeout hours must be greater than 0")
                .LessThanOrEqualTo(8760).WithMessage("Timeout hours cannot exceed 8760 (1 year)")
                .When(x => x.TimeoutHours.HasValue);
        }

        private bool BeValidStepType(string stepType)
        {
            var validTypes = new[] { "Sign", "Review", "Approve", "Acknowledge" };
            return validTypes.Contains(stepType);
        }

        private bool BeValidAssigneeType(string assigneeType)
        {
            var validTypes = new[] { "User", "Role", "Email" };
            return validTypes.Contains(assigneeType);
        }

        private bool BeValidAuthenticationMethod(string method)
        {
            var validMethods = new[] { "Password", "OTP", "Nafath", "Certificate", "Biometric" };
            return validMethods.Contains(method);
        }
    }

    public class CreateESignatureTemplateRequestValidator : AbstractValidator<CreateESignatureTemplateRequest>
    {
        public CreateESignatureTemplateRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(200).WithMessage("Template name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");

            RuleFor(x => x.TemplateUrl)
                .NotEmpty().WithMessage("Template URL is required")
                .Must(BeValidUrl).WithMessage("Template URL must be a valid URL");

            RuleFor(x => x.TemplateFormat)
                .Must(BeValidTemplateFormat).WithMessage("Invalid template format")
                .When(x => !string.IsNullOrEmpty(x.TemplateFormat));

            RuleFor(x => x.DefaultAuthenticationMethod)
                .Must(BeValidAuthenticationMethod).WithMessage("Invalid authentication method")
                .When(x => !string.IsNullOrEmpty(x.DefaultAuthenticationMethod));

            RuleFor(x => x.DefaultExpiryDays)
                .GreaterThan(0).WithMessage("Default expiry days must be greater than 0")
                .LessThanOrEqualTo(365).WithMessage("Default expiry days cannot exceed 365")
                .When(x => x.DefaultExpiryDays.HasValue);

            RuleFor(x => x.DefaultSigningInstructions)
                .MaximumLength(1000).WithMessage("Default signing instructions cannot exceed 1000 characters");

            RuleFor(x => x.Language)
                .Must(BeValidLanguage).WithMessage("Invalid language code")
                .When(x => !string.IsNullOrEmpty(x.Language));

            RuleFor(x => x.CalendarType)
                .Must(BeValidCalendarType).WithMessage("Invalid calendar type")
                .When(x => !string.IsNullOrEmpty(x.CalendarType));

            RuleForEach(x => x.Fields).SetValidator(new CreateESignatureTemplateFieldRequestValidator());
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        private bool BeValidTemplateFormat(string format)
        {
            var validFormats = new[] { "PDF", "DOCX", "HTML" };
            return validFormats.Contains(format);
        }

        private bool BeValidAuthenticationMethod(string method)
        {
            var validMethods = new[] { "Password", "OTP", "Nafath", "Certificate", "Biometric" };
            return validMethods.Contains(method);
        }

        private bool BeValidLanguage(string language)
        {
            var validLanguages = new[] { "en", "ar" };
            return validLanguages.Contains(language);
        }

        private bool BeValidCalendarType(string calendarType)
        {
            var validTypes = new[] { "Gregorian", "Hijri" };
            return validTypes.Contains(calendarType);
        }
    }

    public class CreateESignatureTemplateFieldRequestValidator : AbstractValidator<CreateESignatureTemplateFieldRequest>
    {
        public CreateESignatureTemplateFieldRequestValidator()
        {
            RuleFor(x => x.FieldName)
                .NotEmpty().WithMessage("Field name is required")
                .MaximumLength(100).WithMessage("Field name cannot exceed 100 characters");

            RuleFor(x => x.FieldType)
                .NotEmpty().WithMessage("Field type is required")
                .Must(BeValidFieldType).WithMessage("Invalid field type");

            RuleFor(x => x.Label)
                .MaximumLength(200).WithMessage("Label cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0")
                .When(x => x.PageNumber.HasValue);

            RuleFor(x => x.PositionX)
                .GreaterThanOrEqualTo(0).WithMessage("Position X must be greater than or equal to 0")
                .When(x => x.PositionX.HasValue);

            RuleFor(x => x.PositionY)
                .GreaterThanOrEqualTo(0).WithMessage("Position Y must be greater than or equal to 0")
                .When(x => x.PositionY.HasValue);

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("Width must be greater than 0")
                .When(x => x.Width.HasValue);

            RuleFor(x => x.Height)
                .GreaterThan(0).WithMessage("Height must be greater than 0")
                .When(x => x.Height.HasValue);

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0");

            RuleFor(x => x.AssignedRole)
                .MaximumLength(50).WithMessage("Assigned role cannot exceed 50 characters");
        }

        private bool BeValidFieldType(string fieldType)
        {
            var validTypes = new[] { "Text", "Date", "Signature", "Checkbox", "Dropdown", "Radio", "Number", "Email", "Phone" };
            return validTypes.Contains(fieldType);
        }
    }

    public class ESignatureListRequestValidator : AbstractValidator<ESignatureListRequest>
    {
        public ESignatureListRequestValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");

            RuleFor(x => x.Status)
                .Must(BeValidDocumentStatus).WithMessage("Invalid document status")
                .When(x => !string.IsNullOrEmpty(x.Status));

            RuleFor(x => x.SortBy)
                .Must(BeValidSortField).WithMessage("Invalid sort field")
                .When(x => !string.IsNullOrEmpty(x.SortBy));

            RuleFor(x => x.SortDirection)
                .Must(BeValidSortDirection).WithMessage("Invalid sort direction")
                .When(x => !string.IsNullOrEmpty(x.SortDirection));

            RuleFor(x => x.Language)
                .Must(BeValidLanguage).WithMessage("Invalid language code")
                .When(x => !string.IsNullOrEmpty(x.Language));
        }

        private bool BeValidDocumentStatus(string status)
        {
            var validStatuses = new[] { "Draft", "Pending", "InProgress", "Completed", "Expired", "Cancelled" };
            return validStatuses.Contains(status);
        }

        private bool BeValidSortField(string sortBy)
        {
            var validFields = new[] { "Title", "Status", "CreatedAt", "UpdatedAt", "ExpiryDate" };
            return validFields.Contains(sortBy);
        }

        private bool BeValidSortDirection(string sortDirection)
        {
            var validDirections = new[] { "asc", "desc" };
            return validDirections.Contains(sortDirection.ToLower());
        }

        private bool BeValidLanguage(string language)
        {
            var validLanguages = new[] { "en", "ar" };
            return validLanguages.Contains(language);
        }
    }
}
