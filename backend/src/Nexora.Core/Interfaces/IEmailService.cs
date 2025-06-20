using System.Threading.Tasks;

namespace Nexora.Core.Interfaces;

public interface IEmailService
{
    Task<string> SendEmailAsync(string to, string subject, string body);
    Task<string> SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName);
    Task<bool> ValidateEmailAsync(string email);
    Task<string> GetEmailStatusAsync(string messageId);
}
