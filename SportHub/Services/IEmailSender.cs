namespace SportHub.Services;

public interface IEmailSender
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl);

    Task SendAccountActivationAsync(
        string recipientEmail,
        string recipientName,
        string activationUrl);
}
