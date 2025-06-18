using System.ComponentModel.DataAnnotations;

namespace Nexora.Core.DTOs;

public class ErrorResponse
{
    public string Message { get; set; }
    public string Code { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = new();
    public string TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class RefreshTokenResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class LogoutResponse
{
    public string Message { get; set; }
    public DateTime LoggedOutAt { get; set; } = DateTime.UtcNow;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthCheckResponse
{
    public string Status { get; set; }
    public Dictionary<string, object> Checks { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
