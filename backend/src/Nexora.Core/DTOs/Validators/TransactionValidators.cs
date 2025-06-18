using FluentValidation;

namespace Nexora.Core.DTOs.Validators
{
    public class CreateTransactionDtoValidator : AbstractValidator<CreateTransactionDto>
    {
        public CreateTransactionDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Amount cannot exceed 1,000,000");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be exactly 3 characters")
                .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be a valid 3-letter ISO code")
                .Must(BeValidCurrency).WithMessage("Invalid currency code");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Transaction type is required")
                .MaximumLength(50).WithMessage("Type must not exceed 50 characters")
                .Must(BeValidTransactionType).WithMessage("Invalid transaction type");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }

        private bool BeValidCurrency(string currency)
        {
            var validCurrencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD" };
            return validCurrencies.Contains(currency);
        }

        private bool BeValidTransactionType(string type)
        {
            var validTypes = new[] { "Payment", "Refund", "Transfer", "Deposit", "Withdrawal", "Fee" };
            return validTypes.Contains(type);
        }
    }

    public class UpdateTransactionStatusDtoValidator : AbstractValidator<UpdateTransactionStatusDto>
    {
        public UpdateTransactionStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .MaximumLength(50).WithMessage("Status must not exceed 50 characters")
                .Must(BeValidStatus).WithMessage("Invalid status");
        }

        private bool BeValidStatus(string status)
        {
            var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed", "Cancelled", "Refunded" };
            return validStatuses.Contains(status);
        }
    }
}
