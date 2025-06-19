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
                .LessThanOrEqualTo(1000000).WithMessage("Amount cannot exceed SAR 1,000,000")
                .Must(BeValidAmount).WithMessage("Amount must have maximum 2 decimal places")
                .Must(NotBeTestAmount).WithMessage("Test amounts are not allowed in production");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be exactly 3 characters")
                .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be a valid 3-letter ISO code")
                .Must(BeValidCurrency).WithMessage("Invalid currency code");

            RuleFor(x => x.ExternalReference)
                .MaximumLength(100).WithMessage("External reference must not exceed 100 characters");

            RuleFor(x => x.IdempotencyKey)
                .NotEmpty().WithMessage("Idempotency key is required for payment security")
                .Length(16, 100).WithMessage("Idempotency key must be between 16 and 100 characters")
                .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Idempotency key can only contain alphanumeric characters, hyphens, and underscores");

            RuleFor(x => x.ExternalReference)
                .Must(NotContainSensitiveData).WithMessage("External reference cannot contain sensitive payment information");
        }

        private bool BeValidPaymentMethod(string paymentMethod)
        {
            var validMethods = new[] { "CreditCard", "DebitCard", "BankTransfer", "PayPal", "Stripe", "ApplePay", "GooglePay", "Mada", "STCPay", "HyperPay" };
            return validMethods.Contains(paymentMethod);
        }

        private bool BeValidCurrency(string currency)
        {
            var validCurrencies = new[] { "SAR", "USD", "EUR", "GBP", "AED", "KWD", "QAR", "BHD", "OMR", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD" };
            return validCurrencies.Contains(currency);
        }

        private bool BeValidAmount(decimal amount)
        {
            return decimal.Round(amount, 2) == amount;
        }

        private bool NotBeTestAmount(decimal amount)
        {
            var testAmounts = new[] { 1.00m, 0.01m, 100.00m, 1000.00m };
            return !testAmounts.Contains(amount);
        }

        private bool NotContainSensitiveData(string data)
        {
            if (string.IsNullOrEmpty(data)) return true;

            var sensitivePatterns = new[]
            {
                @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Card numbers
                @"\b\d{3,4}\b", // CVV
                @"\bpin\b", // PIN references
                @"\bpassword\b", // Password references
            };

            return !sensitivePatterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(data, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
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
