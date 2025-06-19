using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly ITenantService _tenantService;
        private readonly ISmsBillingService _smsBillingService;

        public PaymentsController(
            IPaymentService paymentService, 
            IRepository<Payment> paymentRepository, 
            ITenantService tenantService,
            ISmsBillingService smsBillingService)
        {
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
            _tenantService = tenantService;
            _smsBillingService = smsBillingService;
        }

        [HttpPost]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 402)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _paymentService.ProcessPaymentAsync(request);
                
                return Ok(new PaymentResponse
                {
                    Success = result.Status == "Completed",
                    ReferenceId = result.ExternalReference ?? result.Id.ToString(),
                    Message = result.Status == "Completed" ? "Payment processed successfully" : "Payment processing failed",
                    PaymentId = result.Id,
                    Amount = result.Amount,
                    Currency = result.Currency,
                    Status = result.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while processing the payment"
                });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PaymentDetailsResponse>), 200)]
        public async Task<IActionResult> GetPayments(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var payments = await _paymentService.GetPaymentHistoryAsync(tenantId, startDate, endDate);
                
                var pagedPayments = payments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaymentDetailsResponse
                    {
                        Id = p.Id,
                        TransactionId = p.TransactionId,
                        PaymentMethod = p.PaymentMethod,
                        Amount = p.Amount,
                        Currency = p.Currency,
                        Status = p.Status,
                        ExternalReference = p.ExternalReference,
                        CreatedAt = p.CreatedAt,
                        IdempotencyKey = p.IdempotencyKey
                    })
                    .ToList();

                var result = new PagedResult<PaymentDetailsResponse>
                {
                    Items = pagedPayments,
                    TotalCount = payments.Count,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)payments.Count / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving payments" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PaymentDetailsResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPayment(int id)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(id);
                if (payment == null)
                    return NotFound();

                if (payment.TenantId != _tenantService.GetCurrentTenantId())
                    return NotFound();

                return Ok(new PaymentDetailsResponse
                {
                    Id = payment.Id,
                    TransactionId = payment.TransactionId,
                    PaymentMethod = payment.PaymentMethod,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    ExternalReference = payment.ExternalReference,
                    CreatedAt = payment.CreatedAt,
                    IdempotencyKey = payment.IdempotencyKey
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the payment" });
            }
        }

        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RefundPayment(int id, [FromBody] RefundRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var payment = await _paymentRepository.GetByIdAsync(id);
                if (payment == null)
                    return NotFound();

                if (payment.TenantId != _tenantService.GetCurrentTenantId())
                    return NotFound();

                var refundRequest = new Nexora.Infrastructure.Services.RefundRequest
                {
                    PaymentId = id,
                    Amount = request.Amount,
                    IdempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString("N")
                };

                var result = await _paymentService.RefundPaymentAsync(refundRequest);
                
                return Ok(new PaymentResponse
                {
                    Success = result.Status == "Completed",
                    ReferenceId = result.ExternalReference ?? result.Id.ToString(),
                    Message = result.Status == "Completed" ? "Refund processed successfully" : "Refund processing failed",
                    PaymentId = result.Id,
                    Amount = result.Amount,
                    Currency = result.Currency,
                    Status = result.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while processing the refund"
                });
            }
        }

        #region Subscription Management

        [HttpPost("subscriptions")]
        [ProducesResponseType(typeof(SubscriptionResponse), 201)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var subscription = await _paymentService.CreateSubscriptionAsync(request);
                
                return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, new SubscriptionResponse
                {
                    Id = subscription.Id,
                    PlanName = subscription.PlanName,
                    Amount = subscription.Amount,
                    Currency = subscription.Currency,
                    BillingCycle = subscription.BillingCycle,
                    Status = subscription.Status,
                    StartDate = subscription.StartDate,
                    NextBillingDate = subscription.NextBillingDate,
                    CreatedAt = subscription.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the subscription"
                });
            }
        }

        [HttpGet("subscriptions/{id}")]
        [ProducesResponseType(typeof(SubscriptionResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSubscription(int id)
        {
            try
            {
                return Ok(new { Message = "Subscription retrieval endpoint - implementation pending" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the subscription" });
            }
        }

        [HttpPost("subscriptions/{id}/process-payment")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> ProcessRecurringPayment(int id)
        {
            try
            {
                var payment = await _paymentService.ProcessRecurringPaymentAsync(id);
                
                return Ok(new PaymentResponse
                {
                    Success = payment.Status == "Completed",
                    ReferenceId = payment.ExternalReference ?? payment.Id.ToString(),
                    Message = payment.Status == "Completed" ? "Recurring payment processed successfully" : "Recurring payment processing failed",
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while processing the recurring payment"
                });
            }
        }

        #endregion

        #region SMS Billing

        [HttpPost("sms/calculate-cost")]
        [ProducesResponseType(typeof(SmsBillingResult), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> CalculateSmsUsageCost([FromBody] SmsUsageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                request.TenantId = _tenantService.GetCurrentTenantId();
                var result = await _smsBillingService.CalculateUsageCostAsync(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while calculating SMS usage cost"
                });
            }
        }

        [HttpPost("sms/process-billing")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> ProcessSmsUsageBilling([FromBody] SmsUsageBillingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var payment = await _paymentService.ProcessSmsUsageBillingAsync(request);
                
                return Ok(new PaymentResponse
                {
                    Success = payment.Status == "Completed",
                    ReferenceId = payment.ExternalReference ?? payment.Id.ToString(),
                    Message = payment.Status == "Completed" ? "SMS billing processed successfully" : "SMS billing processing failed",
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while processing SMS billing"
                });
            }
        }

        [HttpGet("sms/usage-summary")]
        [ProducesResponseType(typeof(SmsUsageSummary), 200)]
        public async Task<IActionResult> GetSmsUsageSummary(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var summary = await _smsBillingService.GetUsageSummaryAsync(tenantId, startDate, endDate);
                
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving SMS usage summary" });
            }
        }

        [HttpGet("sms/rate-card")]
        [ProducesResponseType(typeof(List<SmsRateCard>), 200)]
        public async Task<IActionResult> GetSmsRateCard([FromQuery] string country = null)
        {
            try
            {
                var rateCard = await _smsBillingService.GetRateCardAsync(country);
                return Ok(rateCard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving SMS rate card" });
            }
        }

        [HttpPost("sms/estimate-cost")]
        [ProducesResponseType(typeof(SmsCostEstimate), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> EstimateSmsUsageCost([FromBody] SmsEstimateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var estimate = await _smsBillingService.EstimateCostAsync(request);
                return Ok(estimate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while estimating SMS cost" });
            }
        }

        #endregion

        #region Currency Conversion

        [HttpGet("currency/convert")]
        [ProducesResponseType(typeof(CurrencyConversionResponse), 200)]
        public async Task<IActionResult> ConvertCurrency(
            [FromQuery] decimal amount,
            [FromQuery] string fromCurrency,
            [FromQuery] string toCurrency)
        {
            try
            {
                var convertedAmount = await _paymentService.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);
                
                return Ok(new CurrencyConversionResponse
                {
                    OriginalAmount = amount,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    ConvertedAmount = convertedAmount,
                    ConversionRate = convertedAmount / amount,
                    ConversionDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while converting currency" });
            }
        }

        #endregion

        #region Payment Scheduling

        [HttpPost("schedule")]
        [ProducesResponseType(typeof(ScheduledPaymentResponse), 201)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> SchedulePayment([FromBody] SchedulePaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var scheduledPayment = await _paymentService.SchedulePaymentAsync(request);
                
                return CreatedAtAction(nameof(GetScheduledPayment), new { id = scheduledPayment.Id }, new ScheduledPaymentResponse
                {
                    Id = scheduledPayment.Id,
                    Amount = scheduledPayment.Amount,
                    Currency = scheduledPayment.Currency,
                    PaymentMethod = scheduledPayment.PaymentMethod,
                    ScheduledDate = scheduledPayment.ScheduledDate,
                    Status = scheduledPayment.Status,
                    Description = scheduledPayment.Description,
                    CreatedAt = scheduledPayment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while scheduling the payment"
                });
            }
        }

        [HttpGet("schedule/{id}")]
        [ProducesResponseType(typeof(ScheduledPaymentResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetScheduledPayment(int id)
        {
            try
            {
                return Ok(new { Message = "Scheduled payment retrieval endpoint - implementation pending" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the scheduled payment" });
            }
        }

        #endregion

        #region Billing Management

        [HttpGet("billing/current-cycle")]
        [ProducesResponseType(typeof(SmsBillingCycle), 200)]
        public async Task<IActionResult> GetCurrentBillingCycle()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var billingCycle = await _smsBillingService.GetCurrentBillingCycleAsync(tenantId);
                
                return Ok(billingCycle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the current billing cycle" });
            }
        }

        [HttpGet("billing/history")]
        [ProducesResponseType(typeof(List<SmsBillingCycle>), 200)]
        public async Task<IActionResult> GetBillingHistory(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var history = await _smsBillingService.GetBillingHistoryAsync(tenantId, startDate, endDate);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving billing history" });
            }
        }

        [HttpPost("billing/apply-credits")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(SmsBillingResult), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> ApplyCredits([FromBody] ApplyCreditsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                request.TenantId = _tenantService.GetCurrentTenantId();
                var result = await _smsBillingService.ApplyCreditsAsync(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "An error occurred while applying credits"
                });
            }
        }

        [HttpGet("billing/alerts")]
        [ProducesResponseType(typeof(SmsBillingAlert), 200)]
        public async Task<IActionResult> CheckBillingAlerts()
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var alert = await _smsBillingService.CheckBillingAlertsAsync(tenantId);
                
                return Ok(alert);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while checking billing alerts" });
            }
        }

        #endregion
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string ReferenceId { get; set; }
        public string Message { get; set; }
        public int? PaymentId { get; set; }
        public decimal? Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
    }

    public class PaymentDetailsResponse
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string ExternalReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class RefundRequest
    {
        public decimal? Amount { get; set; }
        public string Reason { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class SubscriptionResponse
    {
        public int Id { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string BillingCycle { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextBillingDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CurrencyConversionResponse
    {
        public decimal OriginalAmount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal ConversionRate { get; set; }
        public DateTime ConversionDate { get; set; }
    }

    public class ScheduledPaymentResponse
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
