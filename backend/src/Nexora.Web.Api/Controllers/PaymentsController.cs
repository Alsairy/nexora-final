using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
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

        public PaymentsController(IPaymentService paymentService, IRepository<Payment> paymentRepository, ITenantService tenantService)
        {
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
            _tenantService = tenantService;
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
                
                if (result.Success)
                {
                    return Ok(new PaymentResponse
                    {
                        Success = true,
                        ReferenceId = result.ReferenceId,
                        Message = "Payment processed successfully"
                    });
                }
                else
                {
                    return BadRequest(new PaymentResponse
                    {
                        Success = false,
                        Message = result.ErrorMessage
                    });
                }
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
        public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var payments = await _paymentRepository.GetPagedAsync(page, pageSize);
            return Ok(payments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
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
                CreatedAt = payment.CreatedAt
            });
        }

        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefundPayment(int id, [FromBody] RefundRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            try
            {
                var result = await _paymentService.RefundPaymentAsync(payment.Id, request.Amount, request.Reason);
                
                if (result.Success)
                {
                    return Ok(new PaymentResponse
                    {
                        Success = true,
                        ReferenceId = result.ReferenceId,
                        Message = "Refund processed successfully"
                    });
                }
                else
                {
                    return BadRequest(new PaymentResponse
                    {
                        Success = false,
                        Message = result.ErrorMessage
                    });
                }
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
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string ReferenceId { get; set; }
        public string Message { get; set; }
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
    }

    public class RefundRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }
}
