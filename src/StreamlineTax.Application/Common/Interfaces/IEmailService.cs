namespace StreamlineTax.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, string? plainBody = null);
}