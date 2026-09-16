using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Cryptography;
using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Infrastructure.Services;

public class SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly string _host = configuration["Smtp:Host"] ?? "";
    private readonly int _port = configuration.GetValue<int?>("Smtp:Port") ?? 587;
    private readonly string _username = configuration["Smtp:Username"] ?? "";
    private readonly string _password = configuration["Smtp:Password"] ?? "";
    private readonly string _from = configuration["Smtp:From"] ?? "no-reply@streamlinetax.local";

    private readonly string? _dkimDomain = configuration["Smtp:DkimDomain"];
    private readonly string _dkimSelector = configuration["Smtp:DkimSelector"] ?? "mail";
    private readonly string? _dkimPrivateKeyPath = configuration["Smtp:DkimPrivateKeyPath"];

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string? plainBody = null)
    {
        if (string.IsNullOrWhiteSpace(_host))
        {
            logger.LogInformation(
                "[DEV EMAIL] To: {To} | Subject: {Subject} | Body: {Body}",
                to, subject, htmlBody);
            return;
        }

        logger.LogInformation(
            "Sending email via SMTP {Host}:{Port} to {To} | Subject: {Subject}",
            _host, _port, to, subject);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("StreamlineTax", _from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder();
        builder.HtmlBody = htmlBody;
        if (!string.IsNullOrWhiteSpace(plainBody))
            builder.TextBody = plainBody;
        message.Body = builder.ToMessageBody();

        ApplyDkimSignature(message, to, subject);

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTlsWhenAvailable);
            if (!string.IsNullOrWhiteSpace(_username))
                await client.AuthenticateAsync(_username, _password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (SmtpProtocolException ex)
        {
            logger.LogError(ex, "SMTP protocol error sending email to {To} via {Host}:{Port} - {ErrorMessage}",
                to, _host, _port, ex.Message);
            throw;
        }
        catch (AuthenticationException ex)
        {
            logger.LogError(ex, "Authentication error with SMTP {Host}:{Port}",
                _host, _port);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Email sending to {To} was cancelled or timed out", to);
            throw;
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "IO error sending email to {To} via {Host}:{Port}",
                to, _host, _port);
            throw;
        }
    }

    private void ApplyDkimSignature(MimeMessage message, string to, string subject)
    {
        if (string.IsNullOrWhiteSpace(_dkimDomain) || string.IsNullOrWhiteSpace(_dkimPrivateKeyPath))
            return;

        try
        {
            using var keyFile = File.OpenRead(_dkimPrivateKeyPath);
            var signer = new DkimSigner(keyFile, _dkimDomain, _dkimSelector)
            {
                HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
                BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple
            };

            var headers = new[] { HeaderId.From, HeaderId.To, HeaderId.Subject, HeaderId.Date };
            signer.Sign(message, headers);

            logger.LogInformation("Applied DKIM signature for {Domain}", _dkimDomain);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "DKIM private key file not found at {Path}", _dkimPrivateKeyPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DKIM signing failed for {Domain}; sending without signature", _dkimDomain);
        }
    }
}
