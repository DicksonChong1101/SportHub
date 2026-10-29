namespace SportHub.Services;

// Configuration is read from the Email section in appsettings or User Secrets.
public sealed class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@sporthub.local";
    public string FromName { get; set; } = "SportHub";
    // Optional deployed/tunnel address used inside password-reset emails.
    // Visual Studio Dev Tunnels are detected automatically through VS_TUNNEL_URL.
    public string PublicBaseUrl { get; set; } = string.Empty;
    public bool UseDevelopmentOutboxWhenUnconfigured { get; set; } = true;
}
