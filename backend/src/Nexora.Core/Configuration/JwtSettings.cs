using System;

namespace Nexora.Core.Configuration
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryInMinutes { get; set; } = 60;
        public int RefreshTokenExpiryInDays { get; set; } = 7;
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = true;
        public bool ValidateLifetime { get; set; } = true;
        public bool ValidateIssuerSigningKey { get; set; } = true;
        public bool RequireExpirationTime { get; set; } = true;
        public bool RequireSignedTokens { get; set; } = true;
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}
