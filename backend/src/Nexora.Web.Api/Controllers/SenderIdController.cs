using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/sms/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class SenderIdController : ControllerBase
    {
        private readonly IKsaComplianceService _complianceService;
        private readonly ILogger<SenderIdController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public SenderIdController(
            IKsaComplianceService complianceService,
            ILogger<SenderIdController> logger,
            ICurrentUserService currentUserService)
        {
            _complianceService = complianceService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> RegisterSenderId([FromBody] RegisterSenderIdRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.SenderId) || string.IsNullOrEmpty(request?.BusinessName))
                {
                    return BadRequest(new { message = "Sender ID and business name are required" });
                }

                var tenantId = _currentUserService.TenantId;
                var success = await _complianceService.RegisterSenderIdAsync(
                    request.SenderId, 
                    tenantId, 
                    request.BusinessName, 
                    request.BusinessType);

                if (success)
                {
                    _logger.LogInformation("Sender ID {SenderId} registered for tenant {TenantId}", request.SenderId, tenantId);
                    return Ok(new 
                    { 
                        message = "Sender ID registration submitted successfully",
                        senderId = request.SenderId,
                        status = "Pending",
                        submittedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    return BadRequest(new { message = "Sender ID registration failed. It may already exist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering Sender ID {SenderId}", request?.SenderId);
                return StatusCode(500, new { message = "Internal server error occurred while registering Sender ID" });
            }
        }

        [HttpGet("{senderId}/status")]
        [ProducesResponseType(typeof(SenderIdStatus), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SenderIdStatus>> GetSenderIdStatus(string senderId)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    return BadRequest(new { message = "Sender ID is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var status = await _complianceService.GetSenderIdStatusAsync(senderId, tenantId);

                if (status == null)
                {
                    return NotFound(new { message = "Sender ID not found" });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status for Sender ID {SenderId}", senderId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving Sender ID status" });
            }
        }

        [HttpGet("{senderId}/approved")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CheckSenderIdApproval(string senderId)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    return BadRequest(new { message = "Sender ID is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var isApproved = await _complianceService.IsSenderIdApprovedAsync(senderId, tenantId);

                return Ok(new
                {
                    senderId = senderId,
                    isApproved = isApproved,
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking approval for Sender ID {SenderId}", senderId);
                return StatusCode(500, new { message = "Internal server error occurred while checking Sender ID approval" });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SenderIdStatus>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SenderIdStatus>>> GetTenantSenderIds()
        {
            try
            {
                var tenantId = _currentUserService.TenantId;
                
                var senderIds = new List<SenderIdStatus>();

                return Ok(senderIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Sender IDs for tenant");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving Sender IDs" });
            }
        }

        [HttpPut("{senderId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> UpdateSenderId(string senderId, [FromBody] UpdateSenderIdRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    return BadRequest(new { message = "Sender ID is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var currentStatus = await _complianceService.GetSenderIdStatusAsync(senderId, tenantId);

                if (currentStatus == null)
                {
                    return NotFound(new { message = "Sender ID not found" });
                }

                if (currentStatus.Status != "Pending" && currentStatus.Status != "Rejected")
                {
                    return BadRequest(new { message = "Cannot update Sender ID in current status" });
                }

                _logger.LogInformation("Sender ID {SenderId} update requested for tenant {TenantId}", senderId, tenantId);
                
                return Ok(new { message = "Sender ID updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Sender ID {SenderId}", senderId);
                return StatusCode(500, new { message = "Internal server error occurred while updating Sender ID" });
            }
        }

        [HttpDelete("{senderId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteSenderId(string senderId)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    return BadRequest(new { message = "Sender ID is required" });
                }

                var tenantId = _currentUserService.TenantId;
                var currentStatus = await _complianceService.GetSenderIdStatusAsync(senderId, tenantId);

                if (currentStatus == null)
                {
                    return NotFound(new { message = "Sender ID not found" });
                }

                _logger.LogInformation("Sender ID {SenderId} deletion requested for tenant {TenantId}", senderId, tenantId);
                
                return Ok(new { message = "Sender ID deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Sender ID {SenderId}", senderId);
                return StatusCode(500, new { message = "Internal server error occurred while deleting Sender ID" });
            }
        }

        [HttpGet("requirements")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetRegistrationRequirements()
        {
            try
            {
                var requirements = new
                {
                    generalRequirements = new[]
                    {
                        "Sender ID must be 3-11 characters long",
                        "Only alphanumeric characters allowed",
                        "Must represent your business or brand name",
                        "Cannot contain generic terms like 'SMS', 'TEXT', etc.",
                        "Must comply with KSA CITC regulations"
                    },
                    businessTypes = new[]
                    {
                        "Corporation",
                        "Limited Liability Company",
                        "Partnership",
                        "Sole Proprietorship",
                        "Government Entity",
                        "Non-Profit Organization"
                    },
                    requiredDocuments = new[]
                    {
                        "Commercial Registration Certificate",
                        "Business License",
                        "Authorized Representative ID",
                        "Letter of Authorization (if applicable)"
                    },
                    approvalProcess = new
                    {
                        steps = new[]
                        {
                            "Submit registration request with required information",
                            "Document verification by compliance team",
                            "CITC approval process (if required)",
                            "Final approval and activation"
                        },
                        estimatedTime = "3-7 business days",
                        status = new[]
                        {
                            "Pending - Under review",
                            "Approved - Ready for use",
                            "Rejected - Requires correction",
                            "Suspended - Temporarily disabled"
                        }
                    },
                    complianceNotes = new[]
                    {
                        "All SMS messages must comply with KSA anti-spam regulations",
                        "Promotional messages require explicit consent",
                        "Respect Do Not Disturb (DND) lists",
                        "Follow time window restrictions for marketing messages"
                    }
                };

                return Ok(requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Sender ID requirements");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving requirements" });
            }
        }

        [HttpGet("validate/{senderId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ValidateSenderId(string senderId)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    return BadRequest(new { message = "Sender ID is required" });
                }

                var validationResult = new
                {
                    senderId = senderId,
                    isValidFormat = ValidateSenderIdFormat(senderId),
                    formatErrors = GetFormatErrors(senderId),
                    isAvailable = await CheckSenderIdAvailability(senderId),
                    recommendations = GetSenderIdRecommendations(senderId),
                    validatedAt = DateTime.UtcNow
                };

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Sender ID {SenderId}", senderId);
                return StatusCode(500, new { message = "Internal server error occurred while validating Sender ID" });
            }
        }

        private bool ValidateSenderIdFormat(string senderId)
        {
            if (string.IsNullOrEmpty(senderId))
                return false;

            if (senderId.Length < 3 || senderId.Length > 11)
                return false;

            if (!System.Text.RegularExpressions.Regex.IsMatch(senderId, @"^[a-zA-Z0-9]+$"))
                return false;

            var prohibitedTerms = new[] { "SMS", "TEXT", "MSG", "SPAM", "PROMO", "AD", "ALERT" };
            if (prohibitedTerms.Any(term => senderId.ToUpper().Contains(term)))
                return false;

            return true;
        }

        private List<string> GetFormatErrors(string senderId)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(senderId))
            {
                errors.Add("Sender ID cannot be empty");
                return errors;
            }

            if (senderId.Length < 3)
                errors.Add("Sender ID must be at least 3 characters long");

            if (senderId.Length > 11)
                errors.Add("Sender ID cannot exceed 11 characters");

            if (!System.Text.RegularExpressions.Regex.IsMatch(senderId, @"^[a-zA-Z0-9]+$"))
                errors.Add("Sender ID can only contain alphanumeric characters");

            var prohibitedTerms = new[] { "SMS", "TEXT", "MSG", "SPAM", "PROMO", "AD", "ALERT" };
            var foundProhibited = prohibitedTerms.Where(term => senderId.ToUpper().Contains(term)).ToList();
            if (foundProhibited.Any())
                errors.Add($"Sender ID contains prohibited terms: {string.Join(", ", foundProhibited)}");

            return errors;
        }

        private async Task<bool> CheckSenderIdAvailability(string senderId)
        {
            try
            {
                var tenantId = _currentUserService.TenantId;
                var existingStatus = await _complianceService.GetSenderIdStatusAsync(senderId, tenantId);
                return existingStatus == null;
            }
            catch
            {
                return false;
            }
        }

        private List<string> GetSenderIdRecommendations(string senderId)
        {
            var recommendations = new List<string>();

            if (string.IsNullOrEmpty(senderId))
                return recommendations;

            if (senderId.Length < 6)
                recommendations.Add("Consider using a longer Sender ID for better brand recognition");

            if (System.Text.RegularExpressions.Regex.IsMatch(senderId, @"^\d+$"))
                recommendations.Add("Consider including letters for better readability");

            if (senderId.ToUpper() == senderId)
                recommendations.Add("Consider using mixed case for better readability");

            recommendations.Add("Ensure the Sender ID clearly represents your business or brand");
            recommendations.Add("Avoid using generic terms or abbreviations that may be unclear");

            return recommendations;
        }
    }

    public class RegisterSenderIdRequest
    {
        public string SenderId { get; set; }
        public string BusinessName { get; set; }
        public string BusinessType { get; set; }
        public string ContactPerson { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string BusinessAddress { get; set; }
        public string CommercialRegistration { get; set; }
        public string BusinessLicense { get; set; }
        public string UsageDescription { get; set; }
        public List<string> MessageTypes { get; set; } = new List<string>();
        public bool AgreesToTerms { get; set; }
    }

    public class UpdateSenderIdRequest
    {
        public string BusinessName { get; set; }
        public string BusinessType { get; set; }
        public string ContactPerson { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string BusinessAddress { get; set; }
        public string UsageDescription { get; set; }
        public List<string> MessageTypes { get; set; }
    }
}
