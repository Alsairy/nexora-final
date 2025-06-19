using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/v1/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IWebhookService webhookService,
            ILogger<WebhooksController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost("payment/{provider}")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePaymentWebhook(string provider, [FromBody] object payload)
        {
            try
            {
                var signature = Request.Headers["X-Signature"].FirstOrDefault() ?? 
                               Request.Headers["Stripe-Signature"].FirstOrDefault() ??
                               Request.Headers["PayPal-Transmission-Sig"].FirstOrDefault() ?? "";

                var payloadString = await new StreamReader(Request.Body).ReadToEndAsync();
                
                var result = await _webhookService.ProcessPaymentWebhookAsync(provider, payloadString, signature);
                
                if (result)
                {
                    return Ok(new { message = "Webhook processed successfully" });
                }
                
                return BadRequest(new { message = "Failed to process webhook" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment webhook from provider {Provider}", provider);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("sms/{provider}")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleSmsDeliveryWebhook(string provider, [FromBody] object payload)
        {
            try
            {
                var signature = Request.Headers["X-Signature"].FirstOrDefault() ?? "";
                var payloadString = await new StreamReader(Request.Body).ReadToEndAsync();
                
                var result = await _webhookService.ProcessSmsDeliveryWebhookAsync(provider, payloadString, signature);
                
                if (result)
                {
                    return Ok(new { message = "SMS delivery webhook processed successfully" });
                }
                
                return BadRequest(new { message = "Failed to process SMS delivery webhook" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling SMS delivery webhook from provider {Provider}", provider);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("fraud-detection")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> HandleFraudDetectionWebhook([FromBody] object payload)
        {
            try
            {
                var signature = Request.Headers["X-Nexora-Signature"].FirstOrDefault() ?? "";
                var payloadString = await new StreamReader(Request.Body).ReadToEndAsync();
                
                var result = await _webhookService.ProcessFraudDetectionWebhookAsync(payloadString, signature);
                
                if (result)
                {
                    return Ok(new { message = "Fraud detection webhook processed successfully" });
                }
                
                return BadRequest(new { message = "Failed to process fraud detection webhook" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling fraud detection webhook");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("compliance")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> HandleComplianceWebhook([FromBody] object payload)
        {
            try
            {
                var signature = Request.Headers["X-Nexora-Signature"].FirstOrDefault() ?? "";
                var payloadString = await new StreamReader(Request.Body).ReadToEndAsync();
                
                var result = await _webhookService.ProcessComplianceWebhookAsync(payloadString, signature);
                
                if (result)
                {
                    return Ok(new { message = "Compliance webhook processed successfully" });
                }
                
                return BadRequest(new { message = "Failed to process compliance webhook" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling compliance webhook");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("logs")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetWebhookLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var logs = await _webhookService.GetWebhookLogsAsync(null, fromDate, toDate);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook logs");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("configuration")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetWebhookConfiguration()
        {
            try
            {
                var configuration = await _webhookService.GetWebhookConfigurationAsync();
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook configuration");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("configuration")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateWebhookConfiguration([FromBody] WebhookConfiguration configuration)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _webhookService.UpdateWebhookConfigurationAsync(configuration);
                return Ok(new { message = "Webhook configuration updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating webhook configuration");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("retry/{webhookLogId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RetryFailedWebhook(int webhookLogId)
        {
            try
            {
                var result = await _webhookService.RetryFailedWebhookAsync(webhookLogId);
                
                if (result)
                {
                    return Ok(new { message = "Webhook retry initiated successfully" });
                }
                
                return BadRequest(new { message = "Failed to retry webhook" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying webhook {WebhookLogId}", webhookLogId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("status/{webhookId}")]
        [Authorize]
        public async Task<IActionResult> GetWebhookDeliveryStatus(string webhookId)
        {
            try
            {
                var status = await _webhookService.GetWebhookDeliveryStatusAsync(webhookId);
                return Ok(new { webhookId, status = status.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook delivery status for {WebhookId}", webhookId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> SendWebhook([FromBody] SendWebhookRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _webhookService.SendWebhookAsync(
                    request.Url, 
                    request.Payload, 
                    request.Secret, 
                    request.MaxRetries);
                
                if (result)
                {
                    return Ok(new { message = "Webhook sent successfully" });
                }
                
                return BadRequest(new { message = "Failed to send webhook" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook to {Url}", request?.Url);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    public class SendWebhookRequest
    {
        public string Url { get; set; } = string.Empty;
        public object Payload { get; set; } = new();
        public string Secret { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
    }
}
