using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nexora.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub, INotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly Dictionary<string, UserConnection> _connections;
        private readonly List<NotificationHistory> _notificationHistory;

        public NotificationService(
            IHubContext<NotificationHub, INotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
            _connections = new Dictionary<string, UserConnection>();
            _notificationHistory = new List<NotificationHistory>();
        }

        public async Task SendPaymentStatusNotificationAsync(string tenantId, string userId, PaymentNotification notification)
        {
            try
            {
                _logger.LogInformation("Sending payment status notification for payment {PaymentId} to user {UserId}", 
                    notification.PaymentId, userId);

                var notificationData = new
                {
                    Type = "PaymentStatus",
                    Data = notification,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("PaymentStatusUpdate", notificationData);

                await _hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("PaymentStatusUpdate", notificationData);

                await StoreNotificationHistory(tenantId, userId, NotificationType.PaymentStatus, 
                    "Payment Status Update", $"Payment {notification.PaymentId} status: {notification.Status}");

                _logger.LogInformation("Payment status notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment status notification");
                throw;
            }
        }

        public async Task SendSmsDeliveryNotificationAsync(string tenantId, string userId, SmsDeliveryNotification notification)
        {
            try
            {
                _logger.LogInformation("Sending SMS delivery notification for message {MessageId} to user {UserId}", 
                    notification.MessageId, userId);

                var notificationData = new
                {
                    Type = "SmsDelivery",
                    Data = notification,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("SmsDeliveryUpdate", notificationData);

                await _hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("SmsDeliveryUpdate", notificationData);

                await StoreNotificationHistory(tenantId, userId, NotificationType.SmsDelivery, 
                    "SMS Delivery Update", $"SMS {notification.MessageId} status: {notification.Status}");

                _logger.LogInformation("SMS delivery notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS delivery notification");
                throw;
            }
        }

        public async Task SendPaymentFailureAlertAsync(string tenantId, string userId, PaymentFailureAlert alert)
        {
            try
            {
                _logger.LogWarning("Sending payment failure alert for payment {PaymentId} to user {UserId}", 
                    alert.PaymentId, userId);

                var notificationData = new
                {
                    Type = "PaymentFailure",
                    Data = alert,
                    Timestamp = DateTime.UtcNow,
                    Priority = "High"
                };

                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("PaymentFailureAlert", notificationData);

                await _hubContext.Clients.Group($"tenant_{tenantId}_admins")
                    .SendAsync("PaymentFailureAlert", notificationData);

                await StoreNotificationHistory(tenantId, userId, NotificationType.PaymentFailure, 
                    "Payment Failure Alert", $"Payment {alert.PaymentId} failed: {alert.ErrorMessage}");

                _logger.LogInformation("Payment failure alert sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment failure alert");
                throw;
            }
        }

        public async Task SendBillingThresholdNotificationAsync(string tenantId, string userId, BillingThresholdNotification notification)
        {
            try
            {
                _logger.LogInformation("Sending billing threshold notification for {Service} to user {UserId}", 
                    notification.Service, userId);

                var notificationData = new
                {
                    Type = "BillingThreshold",
                    Data = notification,
                    Timestamp = DateTime.UtcNow,
                    Priority = notification.Severity.ToString()
                };

                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("BillingThresholdAlert", notificationData);

                await _hubContext.Clients.Group($"tenant_{tenantId}_billing")
                    .SendAsync("BillingThresholdAlert", notificationData);

                await StoreNotificationHistory(tenantId, userId, NotificationType.BillingThreshold, 
                    "Billing Threshold Alert", notification.Message);

                _logger.LogInformation("Billing threshold notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send billing threshold notification");
                throw;
            }
        }

        public async Task SendMultiChannelNotificationAsync(string tenantId, string userId, MultiChannelNotification notification)
        {
            try
            {
                _logger.LogInformation("Sending multi-channel notification {NotificationId} to user {UserId}", 
                    notification.NotificationId, userId);

                var notificationData = new
                {
                    Type = "MultiChannel",
                    Data = notification,
                    Timestamp = DateTime.UtcNow,
                    Priority = notification.Priority.ToString()
                };

                if (notification.Channels.Contains(NotificationChannel.WebSocket))
                {
                    await _hubContext.Clients.Group($"user_{userId}")
                        .SendAsync("MultiChannelNotification", notificationData);
                }

                await _hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("MultiChannelNotification", notificationData);

                await StoreNotificationHistory(tenantId, userId, notification.Type, 
                    notification.Title, notification.Message);


                _logger.LogInformation("Multi-channel notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send multi-channel notification");
                throw;
            }
        }

        public async Task SubscribeToNotificationsAsync(string connectionId, string tenantId, string userId)
        {
            try
            {
                _logger.LogInformation("Subscribing connection {ConnectionId} for user {UserId} in tenant {TenantId}", 
                    connectionId, userId, tenantId);

                _connections[connectionId] = new UserConnection
                {
                    ConnectionId = connectionId,
                    TenantId = tenantId,
                    UserId = userId,
                    ConnectedAt = DateTime.UtcNow
                };

                await _hubContext.Groups.AddToGroupAsync(connectionId, $"user_{userId}");
                
                await _hubContext.Groups.AddToGroupAsync(connectionId, $"tenant_{tenantId}");

                _logger.LogInformation("Connection subscribed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to notifications");
                throw;
            }
        }

        public async Task UnsubscribeFromNotificationsAsync(string connectionId)
        {
            try
            {
                if (_connections.TryGetValue(connectionId, out var connection))
                {
                    _logger.LogInformation("Unsubscribing connection {ConnectionId} for user {UserId}", 
                        connectionId, connection.UserId);

                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"user_{connection.UserId}");
                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"tenant_{connection.TenantId}");

                    _connections.Remove(connectionId);

                    _logger.LogInformation("Connection unsubscribed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from notifications");
                throw;
            }
        }

        public async Task<List<NotificationHistory>> GetNotificationHistoryAsync(string tenantId, string userId, int pageSize = 50, int pageNumber = 1)
        {
            try
            {
                _logger.LogInformation("Getting notification history for user {UserId} in tenant {TenantId}", 
                    userId, tenantId);

                var history = _notificationHistory
                    .Where(n => n.TenantId == tenantId && n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return await Task.FromResult(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification history");
                throw;
            }
        }

        private async Task StoreNotificationHistory(string tenantId, string userId, NotificationType type, string title, string message)
        {
            var notification = new NotificationHistory
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTime.UtcNow
            };

            _notificationHistory.Add(notification);

            var userNotifications = _notificationHistory
                .Where(n => n.TenantId == tenantId && n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            if (userNotifications.Count > 1000)
            {
                var toRemove = userNotifications.Skip(1000).ToList();
                foreach (var item in toRemove)
                {
                    _notificationHistory.Remove(item);
                }
            }

            await Task.CompletedTask;
        }

        private class UserConnection
        {
            public string ConnectionId { get; set; } = string.Empty;
            public string TenantId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public DateTime ConnectedAt { get; set; }
        }
    }
}
