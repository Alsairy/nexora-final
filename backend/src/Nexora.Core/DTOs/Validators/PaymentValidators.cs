using FluentValidation;

namespace Nexora.Core.DTOs.Validators
{
    public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentDtoValidator()
        {
            RuleFor(x => x.TransactionId)
                .GreaterThan(0).WithMessage("Transaction ID must be greater than 0");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Payment method is required")
                .MaximumLength(50).WithMessage("Payment method must not exceed 50 characters")
                .Must(BeValidPaymentMethod).WithMessage("Invalid payment method");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Amount cannot exceed 1,000,000");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be exactly 3 characters")
                .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be a valid 3-letter ISO code")
                .Must(BeValidCurrency).WithMessage("Invalid currency code");

            RuleFor(x => x.ExternalReference)
                .MaximumLength(100).WithMessage("External reference must not exceed 100 characters");

            RuleFor(x => x.IdempotencyKey)
                .NotEmpty().WithMessage("Idempotency key is required")
                .MaximumLength(100).WithMessage("Idempotency key must not exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Idempotency key can only contain alphanumeric characters, hyphens, and underscores");
        }

        private bool BeValidPaymentMethod(string paymentMethod)
        {
            var validMethods = new[] { "CreditCard", "DebitCard", "BankTransfer", "PayPal", "Stripe", "ApplePay", "GooglePay", "Cryptocurrency" };
            return validMethods.Contains(paymentMethod);
        }

        private bool BeValidCurrency(string currency)
        {
            var validCurrencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD" };
            return validCurrencies.Contains(currency);
        }
    }

    public class RefundPaymentDtoValidator : AbstractValidator<RefundPaymentDto>
    {
        public RefundPaymentDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Refund amount must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Refund amount cannot exceed 1,000,000");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Refund reason is required")
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
        }
    }
}
