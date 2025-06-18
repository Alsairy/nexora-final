using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System.Threading.Tasks;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TenantsController : ControllerBase
    {
        private readonly IRepository<Tenant> _tenantRepository;

        public TenantsController(IRepository<Tenant> tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [ProducesResponseType(typeof(PagedResponse<TenantResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var tenants = await _tenantRepository.GetPagedAsync(page, pageSize);
            return Ok(tenants);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTenant(int id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound();

            return Ok(new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingTenant = await _tenantRepository.GetAsync(t => t.Subdomain == request.Subdomain);
            if (existingTenant.Any())
                return Conflict(new { message = "Tenant with this subdomain already exists" });

            var tenant = new Tenant
            {
                Name = request.Name,
                Subdomain = request.Subdomain.ToLower(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _tenantRepository.AddAsync(tenant);
            await _tenantRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(int id, [FromBody] UpdateTenantRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound();

            tenant.Name = request.Name;
            tenant.IsActive = request.IsActive;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _tenantRepository.UpdateAsync(tenant);
            await _tenantRepository.SaveChangesAsync();

            return Ok(new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(int id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound();

            await _tenantRepository.DeleteAsync(tenant);
            await _tenantRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateTenant(int id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound();

            tenant.IsActive = true;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _tenantRepository.UpdateAsync(tenant);
            await _tenantRepository.SaveChangesAsync();

            return Ok(new { message = "Tenant activated successfully" });
        }

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateTenant(int id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound();

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _tenantRepository.UpdateAsync(tenant);
            await _tenantRepository.SaveChangesAsync();

            return Ok(new { message = "Tenant deactivated successfully" });
        }
    }

    public class TenantResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; }
        public string Subdomain { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
