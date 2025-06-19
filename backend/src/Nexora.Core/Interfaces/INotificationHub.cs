using System.Threading.Tasks;

namespace Nexora.Core.Interfaces
{
    public interface INotificationHub
    {
        Task JoinTenantGroup(string tenantId);
        Task LeaveTenantGroup(string tenantId);
        Task JoinUserGroup(string userId);
        Task LeaveUserGroup(string userId);
        Task SendToTenant(string tenantId, string method, object data);
        Task SendToUser(string userId, string method, object data);
        Task SendToConnection(string connectionId, string method, object data);
        Task BroadcastToAll(string method, object data);
    }
}
