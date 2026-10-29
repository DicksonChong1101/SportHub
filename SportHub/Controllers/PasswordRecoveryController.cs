using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// ======================================================
// ACCOUNT MODULE - EMAIL PASSWORD RECOVERY
// ======================================================
[AllowAnonymous]
public class PasswordRecoveryController : Controller
{
    private static readonly TimeSpan TokenLifetime =
        TimeSpan.FromMinutes(30);

    private readonly AppDbContext _context;
    private readonly PasswordHashService _passwordHashService;
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<PasswordRecoveryController> _logger;

    public PasswordRecoveryController(
        AppDbContext context,
        PasswordHashService passwordHashService,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailSettings,
        ILogger<PasswordRecoveryController> logger)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _emailSender = emailSender;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.SingleOrDefaultAsync(
            candidate =>
                candidate.Email == normalizedEmail &&
                candidate.IsActive);

        // The same confirmation is returned for every email address so the
        // page never reveals whether a particular account exists.
        if (user is not null)
        {
            var now = DateTime.UtcNow;
            var earlierTokens = await _context.PasswordResetTokens
                .Where(token =>
                    token.UserId == user.UserId &&
                    token.UsedAt == null)
                .ToListAsync();

            foreach (var earlierToken in earlierTokens)
            {
                earlierToken.UsedAt = now;
            }

            var plainToken = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = HashToken(plainToken),
                CreatedAt = now,
                ExpiresAt = now.Add(TokenLifetime)
            });

            await _context.SaveChangesAsync();

            var resetUrl = BuildResetUrl(plainToken);

            if (!string.IsNullOrWhiteSpace(resetUrl))
            {
                try
                {
                    await _emailSender.SendPasswordResetAsync(
                        user.Email,
                        user.FullName,
                        resetUrl);
                }
                catch (Exception exception)
                    when (exception is SmtpException or
                          InvalidOperationException or
                          IOException)
                {
                    // Do not expose email-server details to the visitor.
                    _logger.LogError(
                        exception,
                        "Unable to deliver a password-reset email for user {UserId}.",
                        user.UserId);
                }
            }
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        var resetToken = await FindActiveTokenAsync(token);

        if (resetToken is null)
        {
            return View("ResetPasswordInvalid");
        }

        return View(new ResetPasswordViewModel
        {
            Token = token!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetToken = await FindActiveTokenAsync(model.Token);

        if (resetToken is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "This password-reset link is invalid, expired, or has already been used.");

            return View(model);
        }

        var now = DateTime.UtcNow;
        resetToken.User.PasswordHash =
            _passwordHashService.HashPassword(model.NewPassword);

        resetToken.User.FailedLoginAttempts = 0;
        resetToken.User.LockoutEnd = null;
        resetToken.UsedAt = now;

        // A successful reset also invalidates any other outstanding link.
        var otherTokens = await _context.PasswordResetTokens
            .Where(token =>
                token.UserId == resetToken.UserId &&
                token.PasswordResetTokenId !=
                    resetToken.PasswordResetTokenId &&
                token.UsedAt == null)
            .ToListAsync();

        foreach (var otherToken in otherTokens)
        {
            otherToken.UsedAt = now;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your password has been reset. You can now sign in.";

        return RedirectToAction("Login", "Account");
    }

    private async Task<PasswordResetToken?> FindActiveTokenAsync(
        string? plainToken)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
        {
            return null;
        }

        var tokenHash = HashToken(plainToken);
        var now = DateTime.UtcNow;

        return await _context.PasswordResetTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token =>
                token.TokenHash == tokenHash &&
                token.UsedAt == null &&
                token.ExpiresAt > now &&
                token.User.IsActive);
    }

    private static string HashToken(string plainToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(plainToken)));
    }

    // Uses the active Visual Studio Dev Tunnel so a phone can open the link.
    // A configured PublicBaseUrl is the deployment fallback; localhost is last.
    private string? BuildResetUrl(string plainToken)
    {
        var relativeUrl = Url.Action(
            nameof(ResetPassword),
            "PasswordRecovery",
            new { token = plainToken });

        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return null;
        }

        var externalBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");

        if (string.IsNullOrWhiteSpace(externalBaseUrl))
        {
            externalBaseUrl = _emailSettings.PublicBaseUrl;
        }

        if (Uri.TryCreate(externalBaseUrl, UriKind.Absolute, out var baseUri) &&
            (baseUri.Scheme == Uri.UriSchemeHttps ||
             baseUri.Scheme == Uri.UriSchemeHttp))
        {
            return new Uri(baseUri, relativeUrl.TrimStart('/')).ToString();
        }

        return Url.Action(
            nameof(ResetPassword),
            "PasswordRecovery",
            new { token = plainToken },
            Request.Scheme);
    }
}
