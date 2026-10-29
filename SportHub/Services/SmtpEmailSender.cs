using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;

namespace SportHub.Services;

// ======================================================
// ACCOUNT MODULE - BRANDED SECURITY EMAIL SERVICE
// Password reset and account activation share one SportHub visual template.
// ======================================================
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> settings,
        IWebHostEnvironment environment,
        ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl)
    {
        return SendBrandedActionEmailAsync(
            recipientEmail,
            recipientName,
            resetUrl,
            "Reset your SportHub password",
            "Reset your password",
            "We received a request to reset your SportHub password. This secure link expires in <strong>30 minutes</strong> and can only be used once.",
            "Choose a new password",
            "If you did not request this change, you can safely ignore this email. Your password will remain unchanged.",
            "password reset");
    }

    public Task SendAccountActivationAsync(
        string recipientEmail,
        string recipientName,
        string activationUrl)
    {
        return SendBrandedActionEmailAsync(
            recipientEmail,
            recipientName,
            activationUrl,
            "Activate your SportHub account",
            "Welcome to SportHub",
            "Your member account has been created. Confirm that this email belongs to you before signing in. This secure link expires in <strong>24 hours</strong> and can only be used once.",
            "Activate my account",
            "If you did not register for SportHub, you can safely ignore this email and the account will remain unverified.",
            "account activation");
    }

    private async Task SendBrandedActionEmailAsync(
        string recipientEmail,
        string recipientName,
        string actionUrl,
        string subject,
        string heading,
        string messageText,
        string buttonText,
        string securityNote,
        string operationName)
    {
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeUrl = WebUtility.HtmlEncode(actionUrl);
        const string logoContentId = "sporthub-email-logo";
        var logoPath = Path.Combine(
            _environment.WebRootPath,
            "images",
            "sporthub-logo.png");
        var logoMarkup = File.Exists(logoPath)
            ? $"<img src=\"cid:{logoContentId}\" width=\"88\" height=\"88\" alt=\"SportHub\" style=\"display:block;margin:0 auto 16px;border-radius:50%;object-fit:cover;\" />"
            : "<div style=\"font-size:30px;font-weight:800;color:#176b3a;margin-bottom:16px;\">SportHub</div>";
        var body = BuildBrandedActionEmail(
            safeName,
            safeUrl,
            logoMarkup,
            heading,
            messageText,
            buttonText,
            securityNote);

        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            if (!_settings.UseDevelopmentOutboxWhenUnconfigured)
            {
                throw new InvalidOperationException(
                    "Email SMTP settings have not been configured.");
            }

            await WriteDevelopmentOutboxAsync(
                recipientEmail,
                subject,
                body);

            _logger.LogInformation(
                "SportHub {OperationName} email written to the development outbox for {Recipient}.",
                operationName,
                recipientEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                _settings.FromAddress,
                _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(recipientEmail));

        // Embeds the logo inside the email itself, so it does not depend on
        // localhost or an external image server when opened on a phone.
        if (File.Exists(logoPath))
        {
            var htmlView = AlternateView.CreateAlternateViewFromString(
                body,
                Encoding.UTF8,
                MediaTypeNames.Text.Html);
            var logo = new LinkedResource(logoPath, MediaTypeNames.Image.Png)
            {
                ContentId = logoContentId,
                TransferEncoding = TransferEncoding.Base64
            };

            htmlView.LinkedResources.Add(logo);
            message.AlternateViews.Add(htmlView);
        }

        using var client = new SmtpClient(
            _settings.Host,
            _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_settings.UserName))
        {
            client.Credentials = new NetworkCredential(
                _settings.UserName,
                _settings.Password);
        }

        await client.SendMailAsync(message);
    }

    private static string BuildBrandedActionEmail(
        string safeName,
        string safeUrl,
        string logoMarkup,
        string heading,
        string messageText,
        string buttonText,
        string securityNote)
    {
        return $"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f2f7f3;font-family:Arial,Helvetica,sans-serif;color:#183126;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f2f7f3;padding:28px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:580px;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 14px 40px rgba(19,72,42,.12);">
                      <tr>
                        <td style="height:8px;background:linear-gradient(90deg,#176b3a,#63c174);"></td>
                      </tr>
                      <tr>
                        <td align="center" style="padding:34px 34px 18px;">
                          {logoMarkup}
                          <div style="font-size:13px;letter-spacing:2px;text-transform:uppercase;color:#4b9963;font-weight:700;">Reserve · Play · Enjoy</div>
                          <h1 style="margin:12px 0 0;font-size:28px;line-height:1.25;color:#123b24;">{heading}</h1>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:0 34px 34px;">
                          <p style="font-size:16px;line-height:1.7;margin:8px 0 12px;">Hello {safeName},</p>
                          <p style="font-size:16px;line-height:1.7;margin:0 0 24px;color:#52645a;">{messageText}</p>
                          <table role="presentation" cellspacing="0" cellpadding="0" style="margin:0 auto 26px;">
                            <tr>
                              <td style="background:#198754;border-radius:10px;">
                                <a href="{safeUrl}" style="display:inline-block;padding:14px 28px;color:#ffffff;text-decoration:none;font-size:16px;font-weight:700;">{buttonText}</a>
                              </td>
                            </tr>
                          </table>
                          <div style="padding:16px;background:#f4faf6;border:1px solid #d8eddf;border-radius:12px;">
                            <p style="font-size:13px;line-height:1.5;margin:0 0 6px;color:#607168;">If the button does not open, copy this link into your browser:</p>
                            <p style="font-size:12px;line-height:1.5;margin:0;word-break:break-all;color:#176b3a;">{safeUrl}</p>
                          </div>
                          <p style="font-size:13px;line-height:1.6;margin:24px 0 0;color:#77867e;">{securityNote}</p>
                        </td>
                      </tr>
                      <tr>
                        <td align="center" style="padding:18px 24px;background:#123b24;color:#dcebe1;font-size:12px;">SportHub · Sports Facility Reservation System</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private async Task WriteDevelopmentOutboxAsync(
        string recipientEmail,
        string subject,
        string body)
    {
        var outbox = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "EmailOutbox");

        Directory.CreateDirectory(outbox);

        var safeRecipient = string.Concat(
            recipientEmail.Select(character =>
                char.IsLetterOrDigit(character) ? character : '_'));

        var fileName =
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeRecipient}.html";

        var content = $"""
            <!-- Development-only email preview. Do not deploy this folder publicly. -->
            <meta charset="utf-8" />
            <p><strong>To:</strong> {WebUtility.HtmlEncode(recipientEmail)}</p>
            <p><strong>Subject:</strong> {WebUtility.HtmlEncode(subject)}</p>
            <hr />
            {body}
            """;

        await File.WriteAllTextAsync(
            Path.Combine(outbox, fileName),
            content,
            Encoding.UTF8);
    }
}
