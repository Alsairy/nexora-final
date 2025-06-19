using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationHub>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(
            INotificationService notificationService,
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
                var userId = Context.User?.FindFirst("user_id")?.Value ?? Context.UserIdentifier;

                if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User {UserId} connected to notification hub from tenant {TenantId}", 
                        userId, tenantId);

                    await _notificationService.SubscribeToNotificationsAsync(Context.ConnectionId, tenantId, userId);
                    
                    await Clients.Caller.SendAsync("Connected", new
                    {
                        ConnectionId = Context.ConnectionId,
                        UserId = userId,
                        TenantId = tenantId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("Connection {ConnectionId} missing tenant or user information", 
                        Context.ConnectionId);
                    Context.Abort();
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection setup for {ConnectionId}", Context.ConnectionId);
                Context.Abort();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);

                await _notificationService.UnsubscribeFromNotificationsAsync(Context.ConnectionId);

                if (exception != null)
                {
                    _logger.LogError(exception, "Connection {ConnectionId} disconnected with error", 
                        Context.ConnectionId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnection cleanup for {ConnectionId}", Context.ConnectionId);
            }
        }

        public async Task JoinTenantGroup(string tenantId)
        {
            try
            {
                var userTenantId = Context.User?.FindFirst("tenant_id")?.Value;
                
                if (userTenantId == tenantId)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
                    _logger.LogInformation("Connection {ConnectionId} joined tenant group {TenantId}", 
                        Context.ConnectionId, tenantId);
                }
                else
                {
                    _logger.LogWarning("Unauthorized attempt to join tenant group {TenantId} by connection {ConnectionId}", 
                        tenantId, Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining tenant group {TenantId} for connection {ConnectionId}", 
                    tenantId, Context.ConnectionId);
            }
        }

        public async Task LeaveTenantGroup(string tenantId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
                _logger.LogInformation("Connection {ConnectionId} left tenant group {TenantId}", 
                    Context.ConnectionId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving tenant group {TenantId} for connection {ConnectionId}", 
                    tenantId, Context.ConnectionId);
            }
        }

        public async Task JoinUserGroup(string userId)
        {
            try
            {
                var currentUserId = Context.User?.FindFirst("user_id")?.Value ?? Context.UserIdentifier;
                
                if (currentUserId == userId)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                    _logger.LogInformation("Connection {ConnectionId} joined user group {UserId}", 
                        Context.ConnectionId, userId);
                }
                else
                {
                    _logger.LogWarning("Unauthorized attempt to join user group {UserId} by connection {ConnectionId}", 
                        userId, Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining user group {UserId} for connection {ConnectionId}", 
                    userId, Context.ConnectionId);
            }
        }

        public async Task LeaveUserGroup(string userId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("Connection {ConnectionId} left user group {UserId}", 
                    Context.ConnectionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving user group {UserId} for connection {ConnectionId}", 
                    userId, Context.ConnectionId);
            }
        }

        public async Task SendToTenant(string tenantId, string method, object data)
        {
            try
            {
                var userTenantId = Context.User?.FindFirst("tenant_id")?.Value;
                
                if (userTenantId == tenantId)
                {
                    await Clients.Group($"tenant_{tenantId}").SendAsync(method, data);
                    _logger.LogInformation("Message sent to tenant group {TenantId} via method {Method}", 
                        tenantId, method);
                }
                else
                {
                    _logger.LogWarning("Unauthorized attempt to send to tenant {TenantId} by connection {ConnectionId}", 
                        tenantId, Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to tenant {TenantId} via method {Method}", 
                    tenantId, method);
            }
        }

        public async Task SendToUser(string userId, string method, object data)
        {
            try
            {
                var currentUserId = Context.User?.FindFirst("user_id")?.Value ?? Context.UserIdentifier;
                
                if (currentUserId == userId)
                {
                    await Clients.Group($"user_{userId}").SendAsync(method, data);
                    _logger.LogInformation("Message sent to user group {UserId} via method {Method}", 
                        userId, method);
                }
                else
                {
                    _logger.LogWarning("Unauthorized attempt to send to user {UserId} by connection {ConnectionId}", 
                        userId, Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to user {UserId} via method {Method}", 
                    userId, method);
            }
        }

        public async Task SendToConnection(string connectionId, string method, object data)
        {
            try
            {
                if (Context.ConnectionId == connectionId)
                {
                    await Clients.Client(connectionId).SendAsync(method, data);
                    _logger.LogInformation("Message sent to connection {ConnectionId} via method {Method}", 
                        connectionId, method);
                }
                else
                {
                    _logger.LogWarning("Unauthorized attempt to send to connection {ConnectionId} by {CurrentConnectionId}", 
                        connectionId, Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to connection {ConnectionId} via method {Method}", 
                    connectionId, method);
            }
        }

        public async Task BroadcastToAll(string method, object data)
        {
            try
            {
                var userRole = Context.User?.FindFirst("role")?.Value;
                
                if (userRole == "Admin" || userRole == "SuperAdmin")
                {
                    await Clients.All.SendAsync(method, data);
                    _logger.LogInformation("Broadcast message sent via method {Method} by connection {ConnectionId}", 
                        method, Context.ConnectionId);
                }
                else
                {
                    _logger.LogWarning("Unauthorized broadcast attempt by connection {ConnectionId} with role {Role}", 
                        Context.ConnectionId, userRole);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting message via method {Method}", method);
            }
        }

        public async Task MarkNotificationAsRead(string notificationId)
        {
            try
            {
                _logger.LogInformation("Marking notification {NotificationId} as read for connection {ConnectionId}", 
                    notificationId, Context.ConnectionId);

                await Clients.Caller.SendAsync("NotificationMarkedAsRead", new { NotificationId = notificationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            }
        }

        public async Task GetNotificationHistory(int pageSize = 50, int pageNumber = 1)
        {
            try
            {
                var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
                var userId = Context.User?.FindFirst("user_id")?.Value ?? Context.UserIdentifier;

                if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(userId))
                {
                    var history = await _notificationService.GetNotificationHistoryAsync(tenantId, userId, pageSize, pageNumber);
                    await Clients.Caller.SendAsync("NotificationHistory", history);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification history for connection {ConnectionId}", Context.ConnectionId);
            }
        }
    }
}
