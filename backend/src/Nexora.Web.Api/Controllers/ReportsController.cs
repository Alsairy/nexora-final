using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;
using Nexora.Web.Api.Middleware;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly IExportService _exportService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportingService reportingService,
            IExportService exportService,
            ICurrentUserService currentUserService,
            ILogger<ReportsController> logger)
        {
            _reportingService = reportingService;
            _exportService = exportService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost("transactions")]
        [ProducesResponseType(typeof(TransactionReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TransactionReport>> GenerateTransactionReport([FromBody] TransactionReportRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Generating transaction report for tenant {TenantId}", tenantId);

                var report = await _reportingService.GenerateTransactionReportAsync(tenantId, request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating transaction report");
                return StatusCode(500, "An error occurred while generating the transaction report");
            }
        }

        [HttpPost("sms-usage")]
        [ProducesResponseType(typeof(SmsUsageReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SmsUsageReport>> GenerateSmsUsageReport([FromBody] SmsUsageReportRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Generating SMS usage report for tenant {TenantId}", tenantId);

                var report = await _reportingService.GenerateSmsUsageReportAsync(tenantId, request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SMS usage report");
                return StatusCode(500, "An error occurred while generating the SMS usage report");
            }
        }

        [HttpPost("revenue")]
        [ProducesResponseType(typeof(RevenueReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<RevenueReport>> GenerateRevenueReport([FromBody] RevenueReportRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Generating revenue report for tenant {TenantId}", tenantId);

                var report = await _reportingService.GenerateRevenueReportAsync(tenantId, request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating revenue report");
                return StatusCode(500, "An error occurred while generating the revenue report");
            }
        }

        [HttpPost("compliance")]
        [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin,Compliance")]
        public async Task<ActionResult<ComplianceReport>> GenerateComplianceReport([FromBody] ComplianceReportRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Generating compliance report for tenant {TenantId}", tenantId);

                var report = await _reportingService.GenerateComplianceReportAsync(tenantId, request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance report");
                return StatusCode(500, "An error occurred while generating the compliance report");
            }
        }

        [HttpPost("custom")]
        [ProducesResponseType(typeof(CustomReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CustomReport>> GenerateCustomReport([FromBody] CustomReportRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Generating custom report for tenant {TenantId}", tenantId);

                var report = await _reportingService.GenerateCustomReportAsync(tenantId, request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom report");
                return StatusCode(500, "An error occurred while generating the custom report");
            }
        }

        [HttpPost("{reportId}/export/pdf")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportReportToPdf(string reportId, [FromBody] ExportOptions options)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Exporting report {ReportId} to PDF for tenant {TenantId}", reportId, tenantId);

                var pdfBytes = await _reportingService.ExportReportToPdfAsync(tenantId, reportId, options);
                var fileName = !string.IsNullOrEmpty(options.FileName) ? options.FileName : $"report-{reportId}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report {ReportId} to PDF", reportId);
                return StatusCode(500, "An error occurred while exporting the report");
            }
        }

        [HttpPost("{reportId}/export/excel")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportReportToExcel(string reportId, [FromBody] ExportOptions options)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Exporting report {ReportId} to Excel for tenant {TenantId}", reportId, tenantId);

                var excelBytes = await _reportingService.ExportReportToExcelAsync(tenantId, reportId, options);
                var fileName = !string.IsNullOrEmpty(options.FileName) ? options.FileName : $"report-{reportId}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report {ReportId} to Excel", reportId);
                return StatusCode(500, "An error occurred while exporting the report");
            }
        }

        [HttpPost("{reportId}/export/csv")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportReportToCsv(string reportId, [FromBody] ExportOptions options)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Exporting report {ReportId} to CSV for tenant {TenantId}", reportId, tenantId);

                var csvBytes = await _reportingService.ExportReportToCsvAsync(tenantId, reportId, options);
                var fileName = !string.IsNullOrEmpty(options.FileName) ? options.FileName : $"report-{reportId}.csv";

                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report {ReportId} to CSV", reportId);
                return StatusCode(500, "An error occurred while exporting the report");
            }
        }

        [HttpGet("templates")]
        [ProducesResponseType(typeof(List<ReportTemplate>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ReportTemplate>>> GetReportTemplates()
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                var templates = await _reportingService.GetReportTemplatesAsync(tenantId);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report templates");
                return StatusCode(500, "An error occurred while retrieving report templates");
            }
        }

        [HttpPost("templates")]
        [ProducesResponseType(typeof(ReportTemplate), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReportTemplate>> CreateReportTemplate([FromBody] CreateReportTemplateRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Creating report template for tenant {TenantId}", tenantId);

                var template = await _reportingService.CreateReportTemplateAsync(tenantId, request);
                return CreatedAtAction(nameof(GetReportTemplates), new { id = template.Id }, template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report template");
                return StatusCode(500, "An error occurred while creating the report template");
            }
        }

        [HttpGet("scheduled")]
        [ProducesResponseType(typeof(List<ScheduledReport>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ScheduledReport>>> GetScheduledReports()
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                var scheduledReports = await _reportingService.GetScheduledReportsAsync(tenantId);
                return Ok(scheduledReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scheduled reports");
                return StatusCode(500, "An error occurred while retrieving scheduled reports");
            }
        }

        [HttpPost("scheduled")]
        [ProducesResponseType(typeof(ScheduledReport), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ScheduledReport>> CreateScheduledReport([FromBody] CreateScheduledReportRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Creating scheduled report for tenant {TenantId}", tenantId);

                var scheduledReport = await _reportingService.CreateScheduledReportAsync(tenantId, request);
                return CreatedAtAction(nameof(GetScheduledReports), new { id = scheduledReport.Id }, scheduledReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scheduled report");
                return StatusCode(500, "An error occurred while creating the scheduled report");
            }
        }

        [HttpGet("export-jobs")]
        [ProducesResponseType(typeof(List<ExportJob>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExportJob>>> GetExportJobs()
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                var exportJobs = await _exportService.GetExportJobsAsync(tenantId);
                return Ok(exportJobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving export jobs");
                return StatusCode(500, "An error occurred while retrieving export jobs");
            }
        }

        [HttpGet("export-jobs/{jobId}")]
        [ProducesResponseType(typeof(ExportJob), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExportJob>> GetExportJob(string jobId)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                var exportJob = await _exportService.GetExportJobAsync(tenantId, jobId);
                return Ok(exportJob);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Export job {jobId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving export job {JobId}", jobId);
                return StatusCode(500, "An error occurred while retrieving the export job");
            }
        }

        [HttpPost("export-jobs")]
        [ProducesResponseType(typeof(ExportJob), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ExportJob>> CreateExportJob([FromBody] CreateExportJobRequest request)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                _logger.LogInformation("Creating export job for tenant {TenantId}", tenantId);

                var exportJob = await _exportService.CreateExportJobAsync(tenantId, request);
                return CreatedAtAction(nameof(GetExportJob), new { jobId = exportJob.Id }, exportJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating export job");
                return StatusCode(500, "An error occurred while creating the export job");
            }
        }

        [HttpDelete("export-jobs/{jobId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelExportJob(string jobId)
        {
            try
            {
                var tenantId = HttpContext.GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is required");
                }

                var cancelled = await _exportService.CancelExportJobAsync(tenantId, jobId);
                if (!cancelled)
                {
                    return NotFound($"Export job {jobId} not found or cannot be cancelled");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling export job {JobId}", jobId);
                return StatusCode(500, "An error occurred while cancelling the export job");
            }
        }
    }
}
