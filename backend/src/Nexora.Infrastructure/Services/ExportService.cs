using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;

namespace Nexora.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly Dictionary<string, ExportJob> _exportJobs = new();

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> ExportToPdfAsync<T>(T data, PdfExportOptions options) where T : class
        {
            try
            {
                _logger.LogInformation("Exporting data to PDF format");

                var htmlContent = GenerateHtmlContent(data, options);
                var pdfBytes = await ConvertHtmlToPdf(htmlContent, options);

                _logger.LogInformation("PDF export completed successfully");
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to PDF");
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions options) where T : class
        {
            try
            {
                _logger.LogInformation("Exporting data to Excel format");

                var excelBytes = await GenerateExcelFile(data, options);

                _logger.LogInformation("Excel export completed successfully");
                return excelBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CsvExportOptions options) where T : class
        {
            try
            {
                _logger.LogInformation("Exporting data to CSV format");

                var csvContent = GenerateCsvContent(data, options);
                var csvBytes = options.Encoding.GetBytes(csvContent);

                _logger.LogInformation("CSV export completed successfully");
                return csvBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportToJsonAsync<T>(T data, JsonExportOptions options) where T : class
        {
            try
            {
                _logger.LogInformation("Exporting data to JSON format");

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = options.Indented,
                    PropertyNamingPolicy = options.CamelCase ? JsonNamingPolicy.CamelCase : null,
                    DefaultIgnoreCondition = options.IgnoreNullValues ? 
                        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull : 
                        System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, jsonOptions);

                _logger.LogInformation("JSON export completed successfully");
                return jsonBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to JSON");
                throw;
            }
        }

        public async Task<byte[]> ExportToXmlAsync<T>(T data, XmlExportOptions options) where T : class
        {
            try
            {
                _logger.LogInformation("Exporting data to XML format");

                var xmlContent = GenerateXmlContent(data, options);
                var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);

                _logger.LogInformation("XML export completed successfully");
                return xmlBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to XML");
                throw;
            }
        }

        public async Task<string> GenerateReportUrlAsync(string tenantId, string reportId, ExportFormat format, ExportOptions options)
        {
            try
            {
                _logger.LogInformation("Generating report URL for tenant {TenantId}, report {ReportId}", tenantId, reportId);

                var urlId = Guid.NewGuid().ToString();
                var baseUrl = "https://api.nexora.com"; // This would come from configuration
                var reportUrl = $"{baseUrl}/api/reports/{tenantId}/{reportId}/export/{format.ToString().ToLower()}?token={urlId}";

                _logger.LogInformation("Report URL generated successfully");
                return reportUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report URL");
                throw;
            }
        }

        public async Task<List<ExportJob>> GetExportJobsAsync(string tenantId)
        {
            try
            {
                var jobs = _exportJobs.Values
                    .Where(j => j.TenantId == tenantId)
                    .OrderByDescending(j => j.CreatedAt)
                    .ToList();

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving export jobs for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<ExportJob> GetExportJobAsync(string tenantId, string jobId)
        {
            try
            {
                if (_exportJobs.TryGetValue(jobId, out var job) && job.TenantId == tenantId)
                {
                    return job;
                }

                throw new KeyNotFoundException($"Export job {jobId} not found for tenant {tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving export job {JobId} for tenant {TenantId}", jobId, tenantId);
                throw;
            }
        }

        public async Task<ExportJob> CreateExportJobAsync(string tenantId, CreateExportJobRequest request)
        {
            try
            {
                _logger.LogInformation("Creating export job for tenant {TenantId}", tenantId);

                var job = new ExportJob
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    UserId = "current-user", // This would come from the current user service
                    ReportId = request.ReportId,
                    Format = request.Format,
                    Status = ExportJobStatus.Queued,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        { "SendEmailNotification", request.SendEmailNotification },
                        { "EmailAddress", request.EmailAddress ?? string.Empty }
                    }
                };

                _exportJobs[job.Id] = job;

                _ = Task.Run(() => ProcessExportJobAsync(job));

                _logger.LogInformation("Export job {JobId} created successfully", job.Id);
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating export job for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> CancelExportJobAsync(string tenantId, string jobId)
        {
            try
            {
                if (_exportJobs.TryGetValue(jobId, out var job) && job.TenantId == tenantId)
                {
                    if (job.Status == ExportJobStatus.Queued || job.Status == ExportJobStatus.Processing)
                    {
                        job.Status = ExportJobStatus.Cancelled;
                        _logger.LogInformation("Export job {JobId} cancelled successfully", jobId);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling export job {JobId} for tenant {TenantId}", jobId, tenantId);
                throw;
            }
        }

        private string GenerateHtmlContent<T>(T data, PdfExportOptions options) where T : class
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            if (!string.IsNullOrEmpty(options.HeaderText))
            {
                html.AppendLine($"<h1>{options.HeaderText}</h1>");
            }

            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Property</th><th>Value</th></tr>");
            
            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(data)?.ToString() ?? "";
                html.AppendLine($"<tr><td>{prop.Name}</td><td>{value}</td></tr>");
            }
            
            html.AppendLine("</table>");

            if (!string.IsNullOrEmpty(options.FooterText))
            {
                html.AppendLine($"<footer>{options.FooterText}</footer>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private async Task<byte[]> ConvertHtmlToPdf(string htmlContent, PdfExportOptions options)
        {

            var mockPdfContent = $"PDF Content: {htmlContent.Substring(0, Math.Min(100, htmlContent.Length))}...";
            return Encoding.UTF8.GetBytes(mockPdfContent);
        }

        private async Task<byte[]> GenerateExcelFile<T>(IEnumerable<T> data, ExcelExportOptions options) where T : class
        {

            var csv = GenerateCsvContent(data, new CsvExportOptions { IncludeHeaders = options.IncludeHeaders });
            var mockExcelContent = $"Excel Content (CSV format):\n{csv}";
            return Encoding.UTF8.GetBytes(mockExcelContent);
        }

        private string GenerateCsvContent<T>(IEnumerable<T> data, CsvExportOptions options) where T : class
        {
            var csv = new StringBuilder();
            var properties = typeof(T).GetProperties();

            if (options.IncludeHeaders)
            {
                var headers = properties.Select(p => EscapeCsvValue(p.Name, options));
                csv.AppendLine(string.Join(options.Delimiter, headers));
            }

            foreach (var item in data)
            {
                var values = properties.Select(p => 
                {
                    var value = p.GetValue(item);
                    return EscapeCsvValue(FormatValue(value, options), options);
                });
                csv.AppendLine(string.Join(options.Delimiter, values));
            }

            return csv.ToString();
        }

        private string GenerateXmlContent<T>(T data, XmlExportOptions options) where T : class
        {
            var xml = new StringBuilder();

            if (options.IncludeXmlDeclaration)
            {
                xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            }

            xml.AppendLine($"<{options.RootElementName}>");

            if (data is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable)
                {
                    xml.AppendLine($"  <{options.ItemElementName}>");
                    var properties = item.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item)?.ToString() ?? "";
                        xml.AppendLine($"    <{prop.Name}>{System.Security.SecurityElement.Escape(value)}</{prop.Name}>");
                    }
                    xml.AppendLine($"  </{options.ItemElementName}>");
                }
            }
            else
            {
                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data)?.ToString() ?? "";
                    xml.AppendLine($"  <{prop.Name}>{System.Security.SecurityElement.Escape(value)}</{prop.Name}>");
                }
            }

            xml.AppendLine($"</{options.RootElementName}>");

            return xml.ToString();
        }

        private string EscapeCsvValue(string value, CsvExportOptions options)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(options.Delimiter) || value.Contains(options.TextQualifier) || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace(options.TextQualifier, options.TextQualifier + options.TextQualifier);
                return options.TextQualifier + value + options.TextQualifier;
            }

            return value;
        }

        private string FormatValue(object? value, CsvExportOptions options)
        {
            if (value == null)
                return string.Empty;

            return value switch
            {
                DateTime dateTime => dateTime.ToString(options.DateFormat),
                decimal decimalValue => decimalValue.ToString(options.NumberFormat),
                double doubleValue => doubleValue.ToString(options.NumberFormat),
                float floatValue => floatValue.ToString(options.NumberFormat),
                _ => value.ToString() ?? string.Empty
            };
        }

        private async Task ProcessExportJobAsync(ExportJob job)
        {
            try
            {
                job.Status = ExportJobStatus.Processing;
                job.StartedAt = DateTime.UtcNow;

                await Task.Delay(TimeSpan.FromSeconds(5));

                job.Status = ExportJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.DownloadUrl = $"https://api.nexora.com/downloads/{job.Id}";
                job.FileSizeBytes = 1024 * 50; // 50KB mock file size

                _logger.LogInformation("Export job {JobId} completed successfully", job.Id);
            }
            catch (Exception ex)
            {
                job.Status = ExportJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Export job {JobId} failed", job.Id);
            }
        }
    }
}
