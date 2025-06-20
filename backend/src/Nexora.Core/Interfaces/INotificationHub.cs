using System.Threading.Tasks;

namespace Nexora.Core.Interfaces;

public interface INotificationHub
{
    Task SendNotificationToUserAsync(string userId, string message);
    Task SendNotificationToGroupAsync(string groupName, string message);
    Task SendNotificationToAllAsync(string message);
    Task AddUserToGroupAsync(string userId, string groupName);
    Task RemoveUserFromGroupAsync(string userId, string groupName);
}
