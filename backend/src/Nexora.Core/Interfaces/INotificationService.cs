using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexora.Core.DTOs;
using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces;

public interface INotificationService
{
    Task SendEmailNotificationAsync(string to, string subject, string body);
    Task SendSmsNotificationAsync(string phoneNumber, string message);
    Task SendPushNotificationAsync(string userId, string title, string message);
    Task SendInAppNotificationAsync(string userId, string message);
    Task<bool> ValidateEmailAsync(string email);
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
    Task SendNotificationAsync(NotificationRequest request);
    Task SendEmailAsync(string to, string subject, string body, string? from = null);
    Task SendSmsAsync(string phoneNumber, string message, int tenantId);
    Task<List<NotificationLog>> GetNotificationHistoryAsync(int userId, int page = 1, int pageSize = 20);
    Task<List<NotificationLog>> GetTenantNotificationHistoryAsync(int tenantId, int page = 1, int pageSize = 20);
    Task MarkNotificationAsReadAsync(int notificationId, int userId);
    Task MarkAllNotificationsAsReadAsync(int userId);
    Task<int> GetUnreadNotificationCountAsync(int userId);
    Task<List<NotificationLog>> GetUnreadNotificationsAsync(int userId);
    Task DeleteNotificationAsync(int notificationId, int userId);
    Task DeleteAllNotificationsAsync(int userId);
    Task SendBulkNotificationsAsync(List<int> userIds, string title, string message, string type = "info");
    Task SendTenantBroadcastAsync(int tenantId, string title, string message, string type = "info");
    Task<bool> IsNotificationEnabledAsync(int userId, string notificationType);
    Task UpdateNotificationPreferencesAsync(int userId, Dictionary<string, bool> preferences);
}
