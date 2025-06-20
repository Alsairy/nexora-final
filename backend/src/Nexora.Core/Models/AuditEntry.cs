using Nexora.Core.Entities;

namespace Nexora.Core.Models;

public class AuditEntry
{
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Dictionary<string, object?> Changes { get; set; } = new();
    public Dictionary<string, object?> OriginalValues { get; set; } = new();
    public Dictionary<string, object?> NewValues { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            EntityName = EntityName,
            Action = Action,
            EntityId = EntityId,
            Changes = System.Text.Json.JsonSerializer.Serialize(Changes),
            OriginalValues = System.Text.Json.JsonSerializer.Serialize(OriginalValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(NewValues),
            UserId = UserId,
            TenantId = int.TryParse(TenantId, out var tenantIdInt) ? tenantIdInt : 1,
            Timestamp = Timestamp,
            IpAddress = IpAddress,
            UserAgent = UserAgent,
            SessionId = SessionId
        };
    }
}
