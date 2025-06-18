using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/sms/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class ComplianceController : ControllerBase
    {
        private readonly IKsaComplianceService _complianceService;
        private readonly ILogger<ComplianceController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ComplianceController(
            IKsaComplianceService complianceService,
            ILogger<ComplianceController> logger,
            ICurrentUserService currentUserService)
        {
            _complianceService = complianceService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        [HttpPost("check")]
        [ProducesResponseType(typeof(ComplianceCheckResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ComplianceCheckResult>> CheckCompliance(
            [FromQuery] string phoneNumber,
            [FromQuery] string message,
            [FromQuery] string messageType,
            [FromQuery] string senderId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Phone number, message content, and message type are required" });
                }

                var tenantId = _currentUserService.TenantId;
                var result = await _complianceService.CheckComplianceAsync(phoneNumber, message, messageType, senderId, tenantId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking compliance for {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while checking compliance" });
            }
        }

        [HttpGet("dnd-check")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CheckDndStatus([FromQuery] string phoneNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required" });
                }

                var isDnd = await _complianceService.IsDndNumberAsync(phoneNumber);
                var normalizedNumber = await _complianceService.NormalizePhoneNumberAsync(phoneNumber);

                return Ok(new
                {
                    phoneNumber = phoneNumber,
                    normalizedNumber = normalizedNumber,
                    isDnd = isDnd,
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking DND status for {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while checking DND status" });
            }
        }

        [HttpGet("time-window-check")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CheckTimeWindow(
            [FromQuery] string messageType,
            [FromQuery] DateTime? scheduledTime = null)
        {
            try
            {
                if (string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Message type is required" });
                }

                var isAllowed = await _complianceService.IsTimeWindowAllowedAsync(messageType, scheduledTime);
                var timeWindow = await _complianceService.GetAllowedTimeWindowAsync(messageType);
                var isBusinessHours = await _complianceService.IsBusinessHoursAsync(scheduledTime);

                return Ok(new
                {
                    messageType = messageType,
                    scheduledTime = scheduledTime ?? DateTime.UtcNow,
                    isAllowed = isAllowed,
                    isBusinessHours = isBusinessHours,
                    allowedTimeWindow = timeWindow,
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking time window for message type {MessageType}", messageType);
                return StatusCode(500, new { message = "Internal server error occurred while checking time window" });
            }
        }

        [HttpPost("content-check")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CheckContentCompliance(
            [FromBody] string message,
            [FromQuery] string messageType)
        {
            try
            {
                if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Message content and message type are required" });
                }

                var isCompliant = await _complianceService.IsContentCompliantAsync(message, messageType);
                var isUrlCompliant = await _complianceService.CheckUrlComplianceAsync(message);
                var recommendations = await _complianceService.GetComplianceRecommendationsAsync(message, messageType);

                return Ok(new
                {
                    message = message,
                    messageType = messageType,
                    isContentCompliant = isCompliant,
                    isUrlCompliant = isUrlCompliant,
                    recommendations = recommendations,
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content compliance");
                return StatusCode(500, new { message = "Internal server error occurred while checking content compliance" });
            }
        }

        [HttpGet("prohibited-keywords")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetProhibitedKeywords()
        {
            try
            {
                var keywords = await _complianceService.GetProhibitedKeywordsAsync();
                return Ok(keywords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prohibited keywords");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving prohibited keywords" });
            }
        }

        [HttpGet("report")]
        [ProducesResponseType(typeof(ComplianceReport), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ComplianceReport>> GenerateComplianceReport(
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

                var tenantId = _currentUserService.TenantId;
                var report = await _complianceService.GenerateComplianceReportAsync(tenantId, fromDate, toDate);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance report");
                return StatusCode(500, new { message = "Internal server error occurred while generating compliance report" });
            }
        }

        [HttpGet("violations")]
        [ProducesResponseType(typeof(List<ComplianceViolation>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<ComplianceViolation>>> GetComplianceViolations(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate == default || toDate == default)
                {
                    return BadRequest(new { message = "From date and to date are required" });
                }

                var tenantId = _currentUserService.TenantId;
                var violations = await _complianceService.GetComplianceViolationsAsync(tenantId, fromDate, toDate);

                return Ok(violations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving compliance violations");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving compliance violations" });
            }
        }

        [HttpGet("opt-out-check")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CheckOptOutStatus([FromQuery] string phoneNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var isOptedOut = await _complianceService.IsOptedOutAsync(phoneNumber, tenantId);
                var normalizedNumber = await _complianceService.NormalizePhoneNumberAsync(phoneNumber);

                return Ok(new
                {
                    phoneNumber = phoneNumber,
                    normalizedNumber = normalizedNumber,
                    isOptedOut = isOptedOut,
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking opt-out status for {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while checking opt-out status" });
            }
        }

        [HttpPost("opt-out")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ProcessOptOut([FromBody] string phoneNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var success = await _complianceService.ProcessOptOutRequestAsync(phoneNumber, tenantId);

                if (success)
                {
                    _logger.LogInformation("Opt-out processed for {PhoneNumber}", phoneNumber);
                    return Ok(new { message = "Opt-out request processed successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to process opt-out request" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing opt-out for {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while processing opt-out" });
            }
        }

        [HttpPost("opt-in")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ProcessOptIn([FromBody] string phoneNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var success = await _complianceService.ProcessOptInRequestAsync(phoneNumber, tenantId);

                if (success)
                {
                    _logger.LogInformation("Opt-in processed for {PhoneNumber}", phoneNumber);
                    return Ok(new { message = "Opt-in request processed successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to process opt-in request" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing opt-in for {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while processing opt-in" });
            }
        }

        [HttpPost("dnd-update")]
        [Authorize(Roles = "Admin,TenantAdmin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> UpdateDndList([FromBody] DndUpdateRequest request)
        {
            try
            {
                if (request?.PhoneNumbers == null || !request.PhoneNumbers.Any())
                {
                    return BadRequest(new { message = "Phone numbers list is required" });
                }

                var success = await _complianceService.UpdateDndListAsync(request.PhoneNumbers, request.IsDnd);

                if (success)
                {
                    _logger.LogInformation("DND list updated with {Count} numbers", request.PhoneNumbers.Count);
                    return Ok(new { message = $"DND list updated successfully with {request.PhoneNumbers.Count} numbers" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to update DND list" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DND list");
                return StatusCode(500, new { message = "Internal server error occurred while updating DND list" });
            }
        }

        [HttpGet("dnd-numbers")]
        [Authorize(Roles = "Admin,TenantAdmin")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetDndNumbers()
        {
            try
            {
                var tenantId = _currentUserService.TenantId;
                var dndNumbers = await _complianceService.GetDndNumbersAsync(tenantId);

                return Ok(dndNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving DND numbers");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving DND numbers" });
            }
        }

        [HttpGet("validate-phone")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ValidatePhoneNumber([FromQuery] string phoneNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required" });
                }

                var isValid = await _complianceService.ValidatePhoneNumberFormatAsync(phoneNumber);
                var normalized = await _complianceService.NormalizePhoneNumberAsync(phoneNumber);

                return Ok(new
                {
                    phoneNumber = phoneNumber,
                    normalizedNumber = normalized,
                    isValid = isValid,
                    validatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while validating phone number" });
            }
        }

        [HttpGet("citc-status")]
        [Authorize(Roles = "Admin,TenantAdmin")]
        [ProducesResponseType(typeof(CitcIntegrationStatus), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CitcIntegrationStatus>> GetCitcIntegrationStatus()
        {
            try
            {
                var tenantId = _currentUserService.TenantId;
                var status = await _complianceService.GetCitcIntegrationStatusAsync(tenantId);

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving CITC integration status");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving CITC status" });
            }
        }

        [HttpPost("citc-sync")]
        [Authorize(Roles = "Admin,TenantAdmin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> SyncWithCitc()
        {
            try
            {
                var tenantId = _currentUserService.TenantId;
                var success = await _complianceService.SyncWithCitcAsync(tenantId);

                if (success)
                {
                    _logger.LogInformation("CITC sync completed for tenant {TenantId}", tenantId);
                    return Ok(new { message = "CITC sync completed successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "CITC sync failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing with CITC");
                return StatusCode(500, new { message = "Internal server error occurred while syncing with CITC" });
            }
        }

        [HttpPost("validate-template")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ValidateMessageTemplate(
            [FromBody] string templateContent,
            [FromQuery] string messageType)
        {
            try
            {
                if (string.IsNullOrEmpty(templateContent) || string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Template content and message type are required" });
                }

                var isValid = await _complianceService.ValidateMessageTemplateAsync(templateContent, messageType);
                var recommendations = await _complianceService.GetComplianceRecommendationsAsync(templateContent, messageType);

                return Ok(new
                {
                    templateContent = templateContent,
                    messageType = messageType,
                    isValid = isValid,
                    recommendations = recommendations,
                    validatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating message template");
                return StatusCode(500, new { message = "Internal server error occurred while validating template" });
            }
        }

        [HttpGet("recommendations")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetComplianceRecommendations(
            [FromQuery] string message,
            [FromQuery] string messageType)
        {
            try
            {
                if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Message content and message type are required" });
                }

                var recommendations = await _complianceService.GetComplianceRecommendationsAsync(message, messageType);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance recommendations");
                return StatusCode(500, new { message = "Internal server error occurred while getting recommendations" });
            }
        }
    }

    public class DndUpdateRequest
    {
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public bool IsDnd { get; set; }
    }
}
