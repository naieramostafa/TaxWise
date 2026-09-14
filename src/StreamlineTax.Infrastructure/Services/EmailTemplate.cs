namespace StreamlineTax.Infrastructure.Services;

public static class EmailTemplate
{
    public static (string Html, string Plain) Build(string title, string headline, string message, string actionText, string actionUrl, string? recipientName = null)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName) ? "Hello" : $"Hi {recipientName.Split(' ')[0]},";

        var html = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;font-family:Arial,sans-serif;background-color:#ffffff;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;margin:40px auto;">
            <tr>
              <td style="padding:0 24px;">
                <h1 style="margin:0 0 16px;font-size:22px;color:#1a1a1a;">{headline}</h1>
                <p style="margin:0 0 12px;font-size:15px;color:#333333;">{greeting}</p>
                <p style="margin:0 0 20px;font-size:15px;color:#333333;line-height:1.5;">{message}</p>
                <p style="margin:0 0 20px;">
                  <a href="{actionUrl}" style="display:inline-block;padding:12px 24px;background-color:#4f46e5;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">{actionText}</a>
                </p>
                <p style="margin:0 0 8px;font-size:13px;color:#666666;">If the button doesn't work, copy this link into your browser:</p>
                <p style="margin:0 0 20px;font-size:13px;"><a href="{actionUrl}" style="color:#4f46e5;word-break:break-all;">{actionUrl}</a></p>
                <hr style="margin:20px 0;border:none;border-top:1px solid #eeeeee;">
                <p style="margin:0;font-size:12px;color:#999999;">StreamlineTax &mdash; Tax &amp; Compliance</p>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

        var plain = $"""
        {headline}

        {greeting}

        {message}

        {actionText}: {actionUrl}

        If the link above does not work, copy and paste it into your browser.

        StreamlineTax
        """;

        return (html, plain);
    }
}
