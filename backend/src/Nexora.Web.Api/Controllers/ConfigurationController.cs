using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.Configuration;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationManagementService _configurationService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IConfigurationManagementService configurationService,
            ITenantService tenantService,
            ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _tenantService = tenantService;
            _logger = logger;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<Dictionary<string, object>>> GetConfigurationSummary()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var summary = await _configurationService.GetConfigurationSummaryAsync(tenantId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration summary");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("fintech")]
        public async Task<ActionResult<FintechSettings>> GetFintechSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetFintechSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fintech settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("fintech")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateFintechSettings([FromBody] FintechSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdateFintechSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated fintech settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "Fintech settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid fintech settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fintech settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("payment-providers")]
        public async Task<ActionResult<PaymentProviderSettings>> GetPaymentProviderSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetPaymentProviderSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment provider settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("payment-providers")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdatePaymentProviderSettings([FromBody] PaymentProviderSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdatePaymentProviderSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated payment provider settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "Payment provider settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid payment provider settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment provider settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("payment-providers/{providerName}")]
        public async Task<ActionResult<PaymentProviderConfig>> GetPaymentProviderConfig(string providerName)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var config = await _configurationService.GetPaymentProviderConfigAsync(providerName, tenantId);
                return Ok(config);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment provider config for {ProviderName}", providerName);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("sms-pricing")]
        public async Task<ActionResult<SmsPricingSettings>> GetSmsPricingSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetSmsPricingSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS pricing settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("sms-pricing")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateSmsPricingSettings([FromBody] SmsPricingSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdateSmsPricingSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated SMS pricing settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "SMS pricing settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid SMS pricing settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SMS pricing settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("sms-pricing/tier")]
        public async Task<ActionResult<SmsPricingTier>> GetSmsPricingTier([FromQuery] int volume)
        {
            try
            {
                if (volume <= 0)
                {
                    return BadRequest("Volume must be greater than 0");
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                var tier = await _configurationService.GetSmsPricingTierAsync(volume, tenantId);
                return Ok(tier);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS pricing tier for volume {Volume}", volume);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("sms-pricing/country/{countryCode}")]
        public async Task<ActionResult<SmsCountryPricing>> GetSmsCountryPricing(string countryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
                {
                    return BadRequest("Country code must be a valid 2-letter ISO code");
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                var pricing = await _configurationService.GetSmsCountryPricingAsync(countryCode, tenantId);
                return Ok(pricing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS country pricing for {CountryCode}", countryCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("currency-exchange")]
        public async Task<ActionResult<CurrencyExchangeSettings>> GetCurrencyExchangeSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetCurrencyExchangeSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currency exchange settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("currency-exchange")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateCurrencyExchangeSettings([FromBody] CurrencyExchangeSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdateCurrencyExchangeSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated currency exchange settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "Currency exchange settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid currency exchange settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating currency exchange settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("exchange-rate")]
        public async Task<ActionResult<decimal>> GetExchangeRate([FromQuery] string fromCurrency, [FromQuery] string toCurrency)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
                {
                    return BadRequest("Both fromCurrency and toCurrency are required");
                }

                if (fromCurrency.Length != 3 || toCurrency.Length != 3)
                {
                    return BadRequest("Currency codes must be 3-letter ISO codes");
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                var rate = await _configurationService.GetExchangeRateAsync(fromCurrency, toCurrency, tenantId);
                
                return Ok(new { 
                    fromCurrency, 
                    toCurrency, 
                    rate, 
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("fraud-detection")]
        public async Task<ActionResult<FraudDetectionSettings>> GetFraudDetectionSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetFraudDetectionSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fraud detection settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("fraud-detection")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateFraudDetectionSettings([FromBody] FraudDetectionSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdateFraudDetectionSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated fraud detection settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "Fraud detection settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid fraud detection settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fraud detection settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("compliance")]
        public async Task<ActionResult<ComplianceSettings>> GetComplianceSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetComplianceSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("compliance")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateComplianceSettings([FromBody] ComplianceSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdateComplianceSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated compliance settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "Compliance settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid compliance settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating compliance settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("billing")]
        public async Task<ActionResult<BillingSettings>> GetBillingSettings()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var settings = await _configurationService.GetBillingSettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("billing")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateBillingSettings([FromBody] BillingSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.UpdateBillingSettingsAsync(settings, tenantId);
                
                _logger.LogInformation("Updated billing settings for tenant {TenantId}", tenantId);
                return Ok(new { message = "Billing settings updated successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid billing settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating billing settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("validate/{section}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<bool>> ValidateConfiguration(string section)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var isValid = await _configurationService.IsConfigurationValidAsync(section, tenantId);
                
                return Ok(new { 
                    section, 
                    isValid, 
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration section {Section}", section);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("reset/{section}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ResetToDefaults(string section)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                await _configurationService.ResetToDefaultsAsync(section, tenantId);
                
                _logger.LogInformation("Reset configuration section {Section} to defaults for tenant {TenantId}", 
                    section, tenantId);
                return Ok(new { message = $"Configuration section '{section}' reset to defaults successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting configuration section {Section}", section);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
