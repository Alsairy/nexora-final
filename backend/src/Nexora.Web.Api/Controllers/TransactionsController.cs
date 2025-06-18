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
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITenantService _tenantService;

        public TransactionsController(ITransactionRepository transactionRepository, ITenantService tenantService)
        {
            _transactionRepository = transactionRepository;
            _tenantService = tenantService;
        }

        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [ProducesResponseType(typeof(PagedResponse<TransactionResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string status = "")
        {
            var transactions = await _transactionRepository.GetTransactionsAsync(page, pageSize, status);
            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
                return NotFound();

            return Ok(new TransactionResponse
            {
                Id = transaction.Id,
                ReferenceId = transaction.ReferenceId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Type = transaction.Type,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var transaction = new Transaction
            {
                UserId = currentUserId,
                ReferenceId = Guid.NewGuid().ToString(),
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "Pending",
                Type = request.Type,
                Description = request.Description,
                TenantId = _tenantService.GetCurrentTenantId(),
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, new TransactionResponse
            {
                Id = transaction.Id,
                ReferenceId = transaction.ReferenceId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Type = transaction.Type,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTransactionStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
                return NotFound();

            transaction.Status = request.Status;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            return Ok(new TransactionResponse
            {
                Id = transaction.Id,
                ReferenceId = transaction.ReferenceId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Type = transaction.Type,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTransactions(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserId != userId && currentUserRole != "Admin")
                return Forbid();

            var transactions = await _transactionRepository.GetUserTransactionsAsync(userId, page, pageSize);
            return Ok(transactions);
        }
    }

    public class TransactionResponse
    {
        public int Id { get; set; }
        public string ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTransactionRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }
}
