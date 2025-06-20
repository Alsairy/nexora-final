using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexora.Core.DTOs;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/esignature/[controller]")]
    [Authorize]
    public class TemplatesController : ControllerBase
    {
        private readonly IESignatureTemplateService _templateService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<TemplatesController> _logger;

        public TemplatesController(
            IESignatureTemplateService templateService,
            ICurrentUserService currentUserService,
            ILogger<TemplatesController> logger)
        {
            _templateService = templateService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateESignatureTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var template = await _templateService.CreateTemplateAsync(request, tenantId, userId);
                
                _logger.LogInformation("Template {TemplateId} created successfully by user {UserId}", template.Id, userId);
                
                return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for creating template: {Message}", ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetTemplate(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var template = await _templateService.GetTemplateAsync(id, tenantId);

                if (template == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ESignatureTemplateListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTemplates([FromQuery] ESignatureTemplateListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var templates = await _templateService.GetTemplatesAsync(request, tenantId);

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateESignatureTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var template = await _templateService.UpdateTemplateAsync(id, request, tenantId, userId);
                
                if (template == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                _logger.LogInformation("Template {TemplateId} updated successfully by user {UserId}", id, userId);
                
                return Ok(template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for updating template {TemplateId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var result = await _templateService.DeleteTemplateAsync(id, tenantId, userId);
                
                if (!result)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                _logger.LogInformation("Template {TemplateId} deleted successfully by user {UserId}", id, userId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{templateId}/fields")]
        [ProducesResponseType(typeof(ESignatureTemplateFieldResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> AddTemplateField(int templateId, [FromBody] CreateESignatureTemplateFieldRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var field = await _templateService.AddTemplateFieldAsync(templateId, request, tenantId, userId);
                
                _logger.LogInformation("Field {FieldId} added to template {TemplateId} by user {UserId}", field.Id, templateId, userId);
                
                return CreatedAtAction(nameof(GetTemplateField), new { fieldId = field.Id }, field);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for adding field to template {TemplateId}: {Message}", templateId, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding field to template {TemplateId}", templateId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("fields/{fieldId}")]
        [ProducesResponseType(typeof(ESignatureTemplateFieldResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetTemplateField(int fieldId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var field = await _templateService.GetTemplateFieldAsync(fieldId, tenantId);

                if (field == null)
                    return NotFound(new ErrorResponse { Message = "Template field not found" });

                return Ok(field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template field {FieldId}", fieldId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{templateId}/fields")]
        [ProducesResponseType(typeof(List<ESignatureTemplateFieldResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTemplateFields(int templateId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var fields = await _templateService.GetTemplateFieldsAsync(templateId, tenantId);

                return Ok(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fields for template {TemplateId}", templateId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPut("fields/{fieldId}")]
        [ProducesResponseType(typeof(ESignatureTemplateFieldResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> UpdateTemplateField(int fieldId, [FromBody] UpdateESignatureTemplateFieldRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var field = await _templateService.UpdateTemplateFieldAsync(fieldId, request, tenantId, userId);
                
                if (field == null)
                    return NotFound(new ErrorResponse { Message = "Template field not found" });

                _logger.LogInformation("Template field {FieldId} updated successfully by user {UserId}", fieldId, userId);
                
                return Ok(field);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for updating template field {FieldId}: {Message}", fieldId, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template field {FieldId}", fieldId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpDelete("fields/{fieldId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> RemoveTemplateField(int fieldId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var result = await _templateService.RemoveTemplateFieldAsync(fieldId, tenantId, userId);
                
                if (!result)
                    return NotFound(new ErrorResponse { Message = "Template field not found" });

                _logger.LogInformation("Template field {FieldId} removed successfully by user {UserId}", fieldId, userId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing template field {FieldId}", fieldId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ActivateTemplate(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var template = await _templateService.ActivateTemplateAsync(id, tenantId, userId);
                
                if (template == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                _logger.LogInformation("Template {TemplateId} activated by user {UserId}", id, userId);
                
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> DeactivateTemplate(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var template = await _templateService.DeactivateTemplateAsync(id, tenantId, userId);
                
                if (template == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                _logger.LogInformation("Template {TemplateId} deactivated by user {UserId}", id, userId);
                
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CloneTemplate(int id, [FromBody] CloneTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var template = await _templateService.CloneTemplateAsync(id, request.NewName, tenantId, userId);
                
                if (template == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                _logger.LogInformation("Template {TemplateId} cloned as {NewTemplateId} by user {UserId}", id, template.Id, userId);
                
                return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for cloning template {TemplateId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/create-document")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CreateDocumentFromTemplate(int id, [FromBody] CreateDocumentFromTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.TemplateId = id;
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await _templateService.CreateDocumentFromTemplateAsync(request, tenantId, userId);
                
                _logger.LogInformation("Document {DocumentId} created from template {TemplateId} by user {UserId}", document.Id, id, userId);
                
                return CreatedAtAction("GetDocument", "ESignature", new { id = document.Id }, document);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for creating document from template {TemplateId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document from template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/preview")]
        [ProducesResponseType(typeof(ESignatureTemplatePreviewResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GenerateTemplatePreview(int id, [FromBody] ESignatureTemplatePreviewRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.TemplateId = id;
                var tenantId = int.Parse(_currentUserService.GetTenantId());

                var preview = await _templateService.GenerateTemplatePreviewAsync(request, tenantId);
                
                return Ok(preview);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for generating template preview {TemplateId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template preview {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTemplateCategories()
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var categories = await _templateService.GetTemplateCategoriesAsync(tenantId);

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template categories");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("tags")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTemplateTags()
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var tags = await _templateService.GetTemplateTagsAsync(tenantId);

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template tags");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}/usage-stats")]
        [ProducesResponseType(typeof(ESignatureTemplateUsageStats), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTemplateUsageStats(int id, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var stats = await _templateService.GetTemplateUsageStatsAsync(id, tenantId, fromDate, toDate);

                if (stats == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage stats for template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}/export")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ExportTemplate(int id, [FromQuery] string format = "json")
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var templateBytes = await _templateService.ExportTemplateAsync(id, format, tenantId);

                if (templateBytes == null)
                    return NotFound(new ErrorResponse { Message = "Template not found" });

                var contentType = format.ToLower() switch
                {
                    "xml" => "application/xml",
                    "pdf" => "application/pdf",
                    _ => "application/json"
                };

                return File(templateBytes, contentType, $"template-{id}.{format}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting template {TemplateId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("import")]
        [ProducesResponseType(typeof(ESignatureTemplateResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ImportTemplate([FromBody] ImportTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var templateBytes = Convert.FromBase64String(request.TemplateData);
                var template = await _templateService.ImportTemplateAsync(templateBytes, request.Format, tenantId, userId);
                
                _logger.LogInformation("Template imported successfully by user {UserId}", userId);
                
                return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for importing template: {Message}", ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing template");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(TemplateValidationResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ValidateTemplateDefinition([FromBody] CreateESignatureTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var isValid = await _templateService.ValidateTemplateDefinitionAsync(request, tenantId);
                var errors = await _templateService.GetTemplateValidationErrorsAsync(request, tenantId);

                return Ok(new TemplateValidationResponse
                {
                    IsValid = isValid,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template definition");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }
    }

    public class CloneTemplateRequest
    {
        public string NewName { get; set; }
    }

    public class ImportTemplateRequest
    {
        public string TemplateData { get; set; }
        public string Format { get; set; } = "json";
    }

    public class TemplateValidationResponse
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
