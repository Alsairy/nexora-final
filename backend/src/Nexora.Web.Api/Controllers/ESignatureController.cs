using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ESignatureController : ControllerBase
    {
        private readonly IESignatureService _eSignatureService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ESignatureController> _logger;

        public ESignatureController(
            IESignatureService eSignatureService,
            ICurrentUserService currentUserService,
            ILogger<ESignatureController> logger)
        {
            _eSignatureService = eSignatureService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost("documents")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CreateDocument([FromBody] CreateESignatureDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await _eSignatureService.CreateDocumentAsync(request, tenantId, userId);
                
                _logger.LogInformation("Document {DocumentId} created successfully by user {UserId}", document.Id, userId);
                
                return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for creating document: {Message}", ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents/{id}")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetDocument(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var document = await _eSignatureService.GetDocumentAsync(id, tenantId);

                if (document == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents")]
        [ProducesResponseType(typeof(ESignatureListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetDocuments([FromQuery] ESignatureListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var documents = await _eSignatureService.GetDocumentsAsync(request, tenantId);

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPut("documents/{id}")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateESignatureDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await _eSignatureService.UpdateDocumentAsync(id, request, tenantId, userId);
                
                if (document == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                _logger.LogInformation("Document {DocumentId} updated successfully by user {UserId}", id, userId);
                
                return Ok(document);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for updating document {DocumentId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpDelete("documents/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var result = await _eSignatureService.DeleteDocumentAsync(id, tenantId, userId);
                
                if (!result)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                _logger.LogInformation("Document {DocumentId} deleted successfully by user {UserId}", id, userId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("documents/{id}/send")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> SendDocumentForSigning(int id, [FromBody] SendDocumentForSigningRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.DocumentId = id;
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await _eSignatureService.SendDocumentForSigningAsync(request, tenantId, userId);
                
                if (document == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                _logger.LogInformation("Document {DocumentId} sent for signing by user {UserId}", id, userId);
                
                return Ok(document);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for sending document {DocumentId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document {DocumentId} for signing", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("documents/{id}/cancel")]
        [ProducesResponseType(typeof(ESignatureDocumentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CancelDocument(int id, [FromBody] CancelDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var document = await _eSignatureService.CancelDocumentAsync(id, request.Reason, tenantId, userId);
                
                if (document == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                _logger.LogInformation("Document {DocumentId} cancelled by user {UserId}", id, userId);
                
                return Ok(document);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for cancelling document {DocumentId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("documents/{documentId}/signers")]
        [ProducesResponseType(typeof(ESignatureSignerResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> AddSigner(int documentId, [FromBody] CreateESignatureSignerRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var signer = await _eSignatureService.AddSignerAsync(documentId, request, tenantId, userId);
                
                _logger.LogInformation("Signer {SignerId} added to document {DocumentId} by user {UserId}", signer.Id, documentId, userId);
                
                return CreatedAtAction(nameof(GetSigner), new { signerId = signer.Id }, signer);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for adding signer to document {DocumentId}: {Message}", documentId, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding signer to document {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("signers/{signerId}")]
        [ProducesResponseType(typeof(ESignatureSignerResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSigner(int signerId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var signer = await _eSignatureService.GetSignerAsync(signerId, tenantId);

                if (signer == null)
                    return NotFound(new ErrorResponse { Message = "Signer not found" });

                return Ok(signer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signer {SignerId}", signerId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents/{documentId}/signers")]
        [ProducesResponseType(typeof(List<ESignatureSignerResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetDocumentSigners(int documentId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var signers = await _eSignatureService.GetDocumentSignersAsync(documentId, tenantId);

                return Ok(signers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signers for document {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("signatures")]
        [ProducesResponseType(typeof(ESignatureSignatureResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CreateSignature([FromBody] CreateSignatureRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());

                var signature = await _eSignatureService.CreateSignatureAsync(request, tenantId);
                
                _logger.LogInformation("Signature {SignatureId} created successfully", signature.Id);
                
                return CreatedAtAction(nameof(GetSignature), new { id = signature.Id }, signature);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for creating signature: {Message}", ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating signature");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("signatures/{id}")]
        [ProducesResponseType(typeof(ESignatureSignatureResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetSignature(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var signature = await _eSignatureService.GetSignatureAsync(id, tenantId);

                if (signature == null)
                    return NotFound(new ErrorResponse { Message = "Signature not found" });

                return Ok(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signature {SignatureId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents/{id}/progress")]
        [ProducesResponseType(typeof(ESignatureProgressResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetDocumentProgress(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var progress = await _eSignatureService.GetDocumentProgressAsync(id, tenantId);

                if (progress == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document progress for {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ESignatureStatisticsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var statistics = await _eSignatureService.GetStatisticsAsync(tenantId, fromDate, toDate);

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting e-signature statistics");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents/{id}/pdf")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GenerateDocumentPdf(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var pdfBytes = await _eSignatureService.GenerateDocumentPdfAsync(id, tenantId);

                if (pdfBytes == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                return File(pdfBytes, "application/pdf", $"document-{id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents/{id}/evidence")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GenerateLegalEvidencePackage(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var evidenceBytes = await _eSignatureService.GenerateLegalEvidencePackageAsync(id, tenantId);

                if (evidenceBytes == null)
                    return NotFound(new ErrorResponse { Message = "Document not found" });

                return File(evidenceBytes, "application/zip", $"evidence-package-{id}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating evidence package for document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("documents/{documentId}/signers/{signerId}/signing-link")]
        [ProducesResponseType(typeof(SigningLinkResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GenerateSigningLink(int documentId, int signerId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var signingLink = await _eSignatureService.GenerateSigningLinkAsync(documentId, signerId, tenantId);

                if (string.IsNullOrEmpty(signingLink))
                    return NotFound(new ErrorResponse { Message = "Document or signer not found" });

                return Ok(new SigningLinkResponse { SigningLink = signingLink });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signing link for document {DocumentId}, signer {SignerId}", documentId, signerId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("documents/{id}/audit-trail")]
        [ProducesResponseType(typeof(List<ESignatureAuditLog>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetDocumentAuditTrail(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var auditTrail = await _eSignatureService.GetDocumentAuditTrailAsync(id, tenantId);

                return Ok(auditTrail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit trail for document {DocumentId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }
    }

    public class CancelDocumentRequest
    {
        public string Reason { get; set; }
    }

    public class SigningLinkResponse
    {
        public string SigningLink { get; set; }
    }


}
