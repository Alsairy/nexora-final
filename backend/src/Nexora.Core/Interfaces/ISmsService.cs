using System.Threading.Tasks;

namespace Nexora.Core.Interfaces;

public interface ISmsService
{
    Task<string> SendSmsAsync(string phoneNumber, string message);
    Task<string> GetSmsStatusAsync(string messageId);
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
}
