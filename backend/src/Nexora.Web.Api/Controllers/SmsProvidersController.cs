using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nexora.Core.DTOs;
using Nexora.Core.Interfaces;
using FluentValidation;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/sms/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin,TenantAdmin")]
    public class ProvidersController : ControllerBase
    {
        private readonly ISmsService _smsService;
        private readonly ILogger<ProvidersController> _logger;
        private readonly IValidator<CreateSmsProviderRequest> _createProviderValidator;
        private readonly IValidator<UpdateSmsProviderRequest> _updateProviderValidator;

        public ProvidersController(
            ISmsService smsService,
            ILogger<ProvidersController> logger,
            IValidator<CreateSmsProviderRequest> createProviderValidator,
            IValidator<UpdateSmsProviderRequest> updateProviderValidator)
        {
            _smsService = smsService;
            _logger = logger;
            _createProviderValidator = createProviderValidator;
            _updateProviderValidator = updateProviderValidator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SmsProviderDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SmsProviderDto>>> GetProviders()
        {
            try
            {
                var providers = await _smsService.GetProvidersAsync();
                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SMS providers");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving providers" });
            }
        }

        [HttpGet("{providerId}")]
        [ProducesResponseType(typeof(SmsProviderDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsProviderDto>> GetProvider(int providerId)
        {
            try
            {
                var provider = await _smsService.GetProviderAsync(providerId);
                if (provider == null)
                {
                    return NotFound(new { message = "Provider not found" });
                }

                return Ok(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving provider" });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(SmsProviderDto), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsProviderDto>> CreateProvider([FromBody] CreateSmsProviderRequest request)
        {
            try
            {
                var validationResult = await _createProviderValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.Errors);
                }

                _logger.LogInformation("Creating SMS provider {ProviderName}", request.Name);
                var provider = await _smsService.CreateProviderAsync(request);

                _logger.LogInformation("SMS provider {ProviderName} created with ID {ProviderId}", request.Name, provider.Id);
                return CreatedAtAction(nameof(GetProvider), new { providerId = provider.Id }, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SMS provider {ProviderName}", request.Name);
                return StatusCode(500, new { message = "Internal server error occurred while creating provider" });
            }
        }

        [HttpPut("{providerId}")]
        [ProducesResponseType(typeof(SmsProviderDto), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsProviderDto>> UpdateProvider(int providerId, [FromBody] UpdateSmsProviderRequest request)
        {
            try
            {
                var validationResult = await _updateProviderValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.Errors);
                }

                var provider = await _smsService.UpdateProviderAsync(providerId, request);
                if (provider == null)
                {
                    return NotFound(new { message = "Provider not found" });
                }

                _logger.LogInformation("SMS provider {ProviderId} updated successfully", providerId);
                return Ok(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while updating provider" });
            }
        }

        [HttpDelete("{providerId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteProvider(int providerId)
        {
            try
            {
                var success = await _smsService.DeleteProviderAsync(providerId);
                if (!success)
                {
                    return NotFound(new { message = "Provider not found" });
                }

                _logger.LogInformation("SMS provider {ProviderId} deleted successfully", providerId);
                return Ok(new { message = "Provider deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while deleting provider" });
            }
        }

        [HttpPost("{providerId}/test-connection")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> TestProviderConnection(int providerId)
        {
            try
            {
                var success = await _smsService.TestProviderConnectionAsync(providerId);
                return Ok(new
                {
                    providerId = providerId,
                    connectionTest = success ? "Passed" : "Failed",
                    testedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while testing provider connection" });
            }
        }

        [HttpGet("{providerId}/health")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Dictionary<string, object>>> GetProviderHealth(int providerId)
        {
            try
            {
                var health = await _smsService.GetProviderHealthAsync(providerId);
                if (health == null)
                {
                    return NotFound(new { message = "Provider not found" });
                }

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving provider health" });
            }
        }

        [HttpGet("{providerId}/statistics")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Dictionary<string, object>>> GetProviderStatistics(
            int providerId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "From date and to date are required" });
                }

                if (fromDate > toDate)
                {
                    return BadRequest(new { message = "From date must be before to date" });
                }

                var statistics = await _smsService.GetProviderStatisticsAsync(providerId, fromDate, toDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving provider statistics" });
            }
        }

        [HttpGet("{providerId}/rates")]
        [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetProviderRates(int providerId)
        {
            try
            {
                var rates = await _smsService.GetProviderRatesAsync(providerId);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rates for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving provider rates" });
            }
        }

        [HttpPut("{providerId}/rates")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> UpdateProviderRates(int providerId, [FromBody] Dictionary<string, decimal> rates)
        {
            try
            {
                if (rates == null || rates.Count == 0)
                {
                    return BadRequest(new { message = "Rates data is required" });
                }

                await _smsService.UpdateProviderRatesAsync(providerId, rates);
                _logger.LogInformation("Rates updated for SMS provider {ProviderId}", providerId);
                return Ok(new { message = "Provider rates updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rates for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while updating provider rates" });
            }
        }

        [HttpGet("by-country/{country}")]
        [ProducesResponseType(typeof(List<SmsProviderDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SmsProviderDto>>> GetProvidersByCountry(string country)
        {
            try
            {
                if (string.IsNullOrEmpty(country))
                {
                    return BadRequest(new { message = "Country is required" });
                }

                var providers = await _smsService.GetProvidersByCountryAsync(country);
                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SMS providers for country {Country}", country);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving providers by country" });
            }
        }

        [HttpGet("optimal")]
        [ProducesResponseType(typeof(SmsProviderDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsProviderDto>> GetOptimalProvider(
            [FromQuery] string messageType,
            [FromQuery] string country = "SA",
            [FromQuery] decimal? maxCost = null)
        {
            try
            {
                if (string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Message type is required" });
                }

                var provider = await _smsService.GetOptimalProviderAsync(messageType, country, maxCost);
                if (provider == null)
                {
                    return NotFound(new { message = "No optimal provider found for the specified criteria" });
                }

                return Ok(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding optimal SMS provider for {MessageType} in {Country}", messageType, country);
                return StatusCode(500, new { message = "Internal server error occurred while finding optimal provider" });
            }
        }

        [HttpGet("{providerId}/features")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetProviderSupportedFeatures(int providerId)
        {
            try
            {
                var features = await _smsService.GetProviderSupportedFeaturesAsync(providerId);
                return Ok(features);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supported features for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving provider features" });
            }
        }

        [HttpPost("{providerId}/validate-configuration")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ValidateProviderConfiguration(int providerId)
        {
            try
            {
                var isValid = await _smsService.ValidateProviderConfigurationAsync(providerId);
                return Ok(new
                {
                    providerId = providerId,
                    isValid = isValid,
                    validatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while validating provider configuration" });
            }
        }

        [HttpPost("{providerId}/generate-api-key")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GenerateProviderApiKey(int providerId)
        {
            try
            {
                var apiKey = await _smsService.GenerateProviderApiKeyAsync(providerId);
                if (apiKey == null)
                {
                    return NotFound(new { message = "Provider not found" });
                }

                _logger.LogInformation("New API key generated for SMS provider {ProviderId}", providerId);
                return Ok(new
                {
                    providerId = providerId,
                    apiKey = apiKey,
                    generatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while generating API key" });
            }
        }

        [HttpPost("{providerId}/rotate-credentials")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> RotateProviderCredentials(int providerId)
        {
            try
            {
                var success = await _smsService.RotateProviderCredentialsAsync(providerId);
                if (!success)
                {
                    return NotFound(new { message = "Provider not found" });
                }

                _logger.LogInformation("Credentials rotated for SMS provider {ProviderId}", providerId);
                return Ok(new { message = "Provider credentials rotated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating credentials for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while rotating credentials" });
            }
        }

        [HttpGet("{providerId}/usage")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Dictionary<string, object>>> GetProviderUsageStatistics(
            int providerId,
            [FromQuery] string period = "daily")
        {
            try
            {
                var usage = await _smsService.GetProviderUsageStatisticsAsync(providerId, period);
                return Ok(usage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage statistics for SMS provider {ProviderId}", providerId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving usage statistics" });
            }
        }
    }
}
