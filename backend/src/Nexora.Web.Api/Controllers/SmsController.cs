using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Interfaces;
using FluentValidation;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;
        private readonly ILogger<SmsController> _logger;
        private readonly IValidator<SendSmsRequest> _sendSmsValidator;
        private readonly IValidator<SendBulkSmsRequest> _sendBulkSmsValidator;

        public SmsController(
            ISmsService smsService,
            ILogger<SmsController> logger,
            IValidator<SendSmsRequest> sendSmsValidator,
            IValidator<SendBulkSmsRequest> sendBulkSmsValidator)
        {
            _smsService = smsService;
            _logger = logger;
            _sendSmsValidator = sendSmsValidator;
            _sendBulkSmsValidator = sendBulkSmsValidator;
        }

        [HttpPost("send")]
        [ProducesResponseType(typeof(SmsResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsResponse>> SendSms([FromBody] SendSmsRequest request)
        {
            try
            {
                var validationResult = await _sendSmsValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.Errors);
                }

                _logger.LogInformation("Sending SMS to {PhoneNumber}", request.ToNumber);
                var response = await _smsService.SendSmsAsync(request);

                if (response.Success)
                {
                    _logger.LogInformation("SMS sent successfully with ID {MessageId}", response.MessageId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("SMS sending failed: {ErrorMessage}", response.ErrorMessage);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", request.ToNumber);
                return StatusCode(500, new { message = "Internal server error occurred while sending SMS" });
            }
        }

        [HttpPost("send-bulk")]
        [ProducesResponseType(typeof(BulkSmsResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BulkSmsResponse>> SendBulkSms([FromBody] SendBulkSmsRequest request)
        {
            try
            {
                var validationResult = await _sendBulkSmsValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.Errors);
                }

                _logger.LogInformation("Sending bulk SMS to {RecipientCount} recipients", request.Recipients.Count);
                var response = await _smsService.SendBulkSmsAsync(request);

                _logger.LogInformation("Bulk SMS completed: {SuccessfulMessages}/{TotalMessages} successful", 
                    response.SuccessfulMessages, response.TotalMessages);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk SMS");
                return StatusCode(500, new { message = "Internal server error occurred while sending bulk SMS" });
            }
        }

        [HttpGet("status/{messageId}")]
        [ProducesResponseType(typeof(SmsDeliveryStatusResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsDeliveryStatusResponse>> GetDeliveryStatus(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                {
                    return BadRequest(new { message = "Message ID is required" });
                }

                var status = await _smsService.GetDeliveryStatusAsync(messageId);
                if (status == null)
                {
                    return NotFound(new { message = "Message not found" });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery status for message {MessageId}", messageId);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving delivery status" });
            }
        }

        [HttpGet("messages")]
        [ProducesResponseType(typeof(SmsListResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsListResponse>> GetMessages([FromQuery] SmsListRequest request)
        {
            try
            {
                var response = await _smsService.GetMessagesAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SMS messages");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving messages" });
            }
        }

        [HttpPost("cancel/{messageId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CancelScheduledMessage(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                {
                    return BadRequest(new { message = "Message ID is required" });
                }

                var success = await _smsService.CancelScheduledMessageAsync(messageId);
                if (!success)
                {
                    return NotFound(new { message = "Scheduled message not found or cannot be cancelled" });
                }

                return Ok(new { message = "Message cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling scheduled message {MessageId}", messageId);
                return StatusCode(500, new { message = "Internal server error occurred while cancelling message" });
            }
        }

        [HttpPost("resend/{messageId}")]
        [ProducesResponseType(typeof(SmsResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SmsResponse>> ResendMessage(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                {
                    return BadRequest(new { message = "Message ID is required" });
                }

                var response = await _smsService.ResendMessageAsync(messageId);
                if (!response.Success && response.ErrorCode == "MESSAGE_NOT_FOUND")
                {
                    return NotFound(new { message = "Original message not found" });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending message {MessageId}", messageId);
                return StatusCode(500, new { message = "Internal server error occurred while resending message" });
            }
        }

        [HttpGet("estimate-cost")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> EstimateCost(
            [FromQuery] string message,
            [FromQuery] string messageType,
            [FromQuery] string senderId = null,
            [FromQuery] int? providerId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(messageType))
                {
                    return BadRequest(new { message = "Message content and message type are required" });
                }

                var cost = await _smsService.EstimateMessageCostAsync(message, messageType, senderId, providerId);
                var segments = await _smsService.EstimateMessageSegmentsAsync(message);

                return Ok(new
                {
                    message = message,
                    messageType = messageType,
                    estimatedCost = cost,
                    currency = "SAR",
                    segments = segments,
                    characterCount = message.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating SMS cost");
                return StatusCode(500, new { message = "Internal server error occurred while estimating cost" });
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

                var isValid = await _smsService.ValidatePhoneNumberAsync(phoneNumber);
                var formatted = await _smsService.FormatPhoneNumberAsync(phoneNumber);

                return Ok(new
                {
                    phoneNumber = phoneNumber,
                    isValid = isValid,
                    formattedNumber = formatted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number {PhoneNumber}", phoneNumber);
                return StatusCode(500, new { message = "Internal server error occurred while validating phone number" });
            }
        }

        [HttpGet("supported-countries")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetSupportedCountries()
        {
            try
            {
                var countries = await _smsService.GetSupportedCountriesAsync();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supported countries");
                return StatusCode(500, new { message = "Internal server error occurred while retrieving supported countries" });
            }
        }

        [HttpPost("delivery-receipt")]
        [AllowAnonymous] // This endpoint is called by SMS providers
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ProcessDeliveryReceipt(
            [FromForm] string providerMessageId,
            [FromForm] string status,
            [FromForm] DateTime? deliveredAt = null,
            [FromForm] string errorCode = null,
            [FromForm] string errorMessage = null)
        {
            try
            {
                if (string.IsNullOrEmpty(providerMessageId) || string.IsNullOrEmpty(status))
                {
                    return BadRequest(new { message = "Provider message ID and status are required" });
                }

                await _smsService.ProcessDeliveryReceiptAsync(providerMessageId, status, deliveredAt, errorCode, errorMessage);
                return Ok(new { message = "Delivery receipt processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delivery receipt for {ProviderMessageId}", providerMessageId);
                return StatusCode(500, new { message = "Internal server error occurred while processing delivery receipt" });
            }
        }
    }
}
