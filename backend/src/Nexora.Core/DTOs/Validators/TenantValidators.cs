using FluentValidation;

namespace Nexora.Core.DTOs.Validators
{
    public class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
    {
        public CreateTenantDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tenant name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
                .MinimumLength(2).WithMessage("Name must be at least 2 characters long");

            RuleFor(x => x.Subdomain)
                .NotEmpty().WithMessage("Subdomain is required")
                .MaximumLength(50).WithMessage("Subdomain must not exceed 50 characters")
                .MinimumLength(3).WithMessage("Subdomain must be at least 3 characters long")
                .Matches(@"^[a-z0-9-]+$").WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens")
                .Must(NotStartOrEndWithHyphen).WithMessage("Subdomain cannot start or end with a hyphen")
                .Must(NotContainConsecutiveHyphens).WithMessage("Subdomain cannot contain consecutive hyphens")
                .Must(NotBeReservedSubdomain).WithMessage("This subdomain is reserved and cannot be used");
        }

        private bool NotStartOrEndWithHyphen(string subdomain)
        {
            return !subdomain.StartsWith("-") && !subdomain.EndsWith("-");
        }

        private bool NotContainConsecutiveHyphens(string subdomain)
        {
            return !subdomain.Contains("--");
        }

        private bool NotBeReservedSubdomain(string subdomain)
        {
            var reservedSubdomains = new[] 
            { 
                "www", "api", "admin", "app", "mail", "ftp", "blog", "shop", "store", 
                "support", "help", "docs", "dev", "test", "staging", "prod", "production",
                "nexora", "system", "root", "public", "private", "secure", "ssl", "cdn"
            };
            return !reservedSubdomains.Contains(subdomain.ToLower());
        }
    }

    public class UpdateTenantDtoValidator : AbstractValidator<UpdateTenantDto>
    {
        public UpdateTenantDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tenant name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
                .MinimumLength(2).WithMessage("Name must be at least 2 characters long");
        }
    }
}
