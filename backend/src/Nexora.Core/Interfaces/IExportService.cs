using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportToPdfAsync<T>(T data, PdfExportOptions options) where T : class;
        Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions options) where T : class;
        Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CsvExportOptions options) where T : class;
        Task<byte[]> ExportToJsonAsync<T>(T data, JsonExportOptions options) where T : class;
        Task<byte[]> ExportToXmlAsync<T>(T data, XmlExportOptions options) where T : class;
        Task<string> GenerateReportUrlAsync(string tenantId, string reportId, ExportFormat format, ExportOptions options);
        Task<List<ExportJob>> GetExportJobsAsync(string tenantId);
        Task<ExportJob> GetExportJobAsync(string tenantId, string jobId);
        Task<ExportJob> CreateExportJobAsync(string tenantId, CreateExportJobRequest request);
        Task<bool> CancelExportJobAsync(string tenantId, string jobId);
    }

    public class PdfExportOptions : ExportOptions
    {
        public PdfPageSize PageSize { get; set; } = PdfPageSize.A4;
        public PdfOrientation Orientation { get; set; } = PdfOrientation.Portrait;
        public PdfMargins Margins { get; set; } = new();
        public string? HeaderText { get; set; }
        public string? FooterText { get; set; }
        public bool IncludePageNumbers { get; set; } = true;
        public string? LogoUrl { get; set; }
        public PdfStyling Styling { get; set; } = new();
    }

    public class ExcelExportOptions : ExportOptions
    {
        public string WorksheetName { get; set; } = "Report";
        public bool IncludeHeaders { get; set; } = true;
        public bool AutoFitColumns { get; set; } = true;
        public bool IncludeFilters { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public ExcelStyling Styling { get; set; } = new();
        public List<ExcelWorksheet> AdditionalWorksheets { get; set; } = new();
    }

    public class CsvExportOptions : ExportOptions
    {
        public string Delimiter { get; set; } = ",";
        public string TextQualifier { get; set; } = "\"";
        public bool IncludeHeaders { get; set; } = true;
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string NumberFormat { get; set; } = "F2";
        public System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
    }

    public class JsonExportOptions : ExportOptions
    {
        public bool Indented { get; set; } = true;
        public bool CamelCase { get; set; } = true;
        public bool IgnoreNullValues { get; set; } = true;
        public string DateFormat { get; set; } = "yyyy-MM-ddTHH:mm:ssZ";
    }

    public class XmlExportOptions : ExportOptions
    {
        public bool Indented { get; set; } = true;
        public string RootElementName { get; set; } = "Report";
        public string ItemElementName { get; set; } = "Item";
        public bool IncludeXmlDeclaration { get; set; } = true;
        public string? Namespace { get; set; }
    }

    public class ExportJob
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ReportId { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public ExportJobStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public long? FileSizeBytes { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CreateExportJobRequest
    {
        public string ReportId { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public Dictionary<string, object> Options { get; set; } = new();
        public bool SendEmailNotification { get; set; } = false;
        public string? EmailAddress { get; set; }
    }

    public class PdfMargins
    {
        public float Top { get; set; } = 20;
        public float Right { get; set; } = 20;
        public float Bottom { get; set; } = 20;
        public float Left { get; set; } = 20;
    }

    public class PdfStyling
    {
        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; } = 10;
        public string HeaderColor { get; set; } = "#333333";
        public string TextColor { get; set; } = "#000000";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public bool AlternateRowColors { get; set; } = true;
    }

    public class ExcelStyling
    {
        public string HeaderBackgroundColor { get; set; } = "#4472C4";
        public string HeaderTextColor { get; set; } = "#FFFFFF";
        public bool BoldHeaders { get; set; } = true;
        public bool AlternateRowColors { get; set; } = true;
        public string AlternateRowColor { get; set; } = "#F2F2F2";
    }

    public class ExcelWorksheet
    {
        public string Name { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public enum ExportFormat
    {
        Pdf,
        Excel,
        Csv,
        Json,
        Xml
    }

    public enum ExportJobStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum PdfPageSize
    {
        A4,
        A3,
        Letter,
        Legal
    }

    public enum PdfOrientation
    {
        Portrait,
        Landscape
    }
}
