// Claims store the signed-in user's ID, name, email address, and role.
using System.Security.Claims;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
// Authentication provides SignInAsync() and SignOutAsync().
using Microsoft.AspNetCore.Authentication;
// Specifies cookie authentication as the application's authentication scheme.
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

public class AccountController : Controller
{
    // Locks an account for 15 minutes after five consecutive failed login attempts.
    private const int MaxFailedLoginAttempts = 5;
    private const long MaxProfileImageBytes = 2 * 1024 * 1024;
    private const string ProfileImageClaimType = "SportHub.ProfileImagePath";
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ActivationTokenLifetime = TimeSpan.FromHours(24);
    private static readonly string[] AllowedProfileImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly PasswordHashService _passwordHashService;
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AccountController> _logger;

    // The controller receives AppDbContext through dependency injection.
    public AccountController(
        AppDbContext context,
        IWebHostEnvironment environment,
        PasswordHashService passwordHashService,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailSettings,
        ILogger<AccountController> logger)
    {
        _context = context;
        _environment = environment;
        _passwordHashService = passwordHashService;
        _emailSender = emailSender;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Normalizes casing and whitespace so equivalent emails cannot register twice.
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        // Provides a user-friendly validation error before the unique index is reached.
        if (await _context.Users.AnyAsync(user => user.Email == normalizedEmail))
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "An account with this email address already exists.");

            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim(),
            // Public registration always creates a Member, never an Admin.
            Role = UserRoles.Member,
            IsActive = true,
            EmailConfirmed = false
        };

        user.PasswordHash = _passwordHashService.HashPassword(model.Password);

        var plainActivationToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));

        user.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            TokenHash = HashSecurityToken(plainActivationToken),
            ExpiresAt = DateTime.UtcNow.Add(ActivationTokenLifetime)
        });

        // Adds the new entity and persists it to the Users table.
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SendActivationEmailSafelyAsync(user, plainActivationToken);

        return RedirectToAction(
            nameof(RegisterConfirmation),
            new { email = user.Email });
    }

    // Tells the visitor to check email without signing in an unverified account.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RegisterConfirmation(string? email = null)
    {
        ViewData["Email"] = email;
        return View();
    }

    // Consumes a single-use activation token and confirms email ownership.
    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ActivateEmail(string? token)
    {
        var verificationToken = await FindActiveVerificationTokenAsync(token);

        if (verificationToken is null)
        {
            ViewData["ActivationSucceeded"] = false;
            return View("ActivationResult");
        }

        var now = DateTime.UtcNow;
        verificationToken.User.EmailConfirmed = true;
        verificationToken.UsedAt = now;

        var otherTokens = await _context.EmailVerificationTokens
            .Where(candidate =>
                candidate.UserId == verificationToken.UserId &&
                candidate.EmailVerificationTokenId != verificationToken.EmailVerificationTokenId &&
                candidate.UsedAt == null)
            .ToListAsync();

        foreach (var otherToken in otherTokens)
        {
            otherToken.UsedAt = now;
        }

        await _context.SaveChangesAsync();
        ViewData["ActivationSucceeded"] = true;
        return View("ActivationResult");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResendActivation(string? email = null)
    {
        return View(new ResendActivationViewModel
        {
            Email = email ?? string.Empty
        });
    }

    // Always returns the same confirmation so registered emails are not exposed.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendActivation(ResendActivationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.SingleOrDefaultAsync(candidate =>
            candidate.Email == normalizedEmail &&
            candidate.IsActive &&
            !candidate.EmailConfirmed);

        if (user is not null)
        {
            var now = DateTime.UtcNow;
            var earlierTokens = await _context.EmailVerificationTokens
                .Where(candidate =>
                    candidate.UserId == user.UserId &&
                    candidate.UsedAt == null)
                .ToListAsync();

            foreach (var earlierToken in earlierTokens)
            {
                earlierToken.UsedAt = now;
            }

            var plainToken = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));

            _context.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.UserId,
                TokenHash = HashSecurityToken(plainToken),
                ExpiresAt = now.Add(ActivationTokenLifetime)
            });

            await _context.SaveChangesAsync();
            await SendActivationEmailSafelyAsync(user, plainToken);
        }

        return RedirectToAction(
            nameof(RegisterConfirmation),
            new { email = normalizedEmail });
    }

    // Displays the login form without requiring an existing authenticated session.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail);

        // The public error stays deliberately generic so attackers cannot discover accounts.
        const string genericLoginError =
            "Unable to sign in. Check your credentials or try again later.";

        if (user is null)
        {
            AddLoginAttempt(null, normalizedEmail, false, "Account not found");
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, genericLoginError);
            return View(model);
        }

        if (!user.IsActive)
        {
            AddLoginAttempt(user, normalizedEmail, false, "Account disabled");
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, genericLoginError);
            return View(model);
        }

        var now = DateTime.UtcNow;

        if (user.LockoutEnd > now)
        {
            AddLoginAttempt(user, normalizedEmail, false, "Account locked");
            await _context.SaveChangesAsync();
            ModelState.AddModelError(
                string.Empty,
                BuildLockoutMessage(user.LockoutEnd.Value));
            return View(model);
        }

        // Once an earlier lockout has expired, a new sequence of attempts begins.
        if (user.LockoutEnd is not null)
        {
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
        }

        var passwordIsValid = _passwordHashService.VerifyPassword(
            user.PasswordHash,
            model.Password,
            out var needsRehash);

        if (!passwordIsValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEnd = now.Add(LockoutDuration);
            }

            AddLoginAttempt(user, normalizedEmail, false, "Invalid password");
            await _context.SaveChangesAsync();
            ModelState.AddModelError(
                string.Empty,
                user.LockoutEnd > now
                    ? BuildLockoutMessage(user.LockoutEnd.Value)
                    : genericLoginError);
            return View(model);
        }

        // Reveals activation status only after the correct password is supplied.
        if (!user.EmailConfirmed)
        {
            AddLoginAttempt(user, normalizedEmail, false, "Email not confirmed");
            await _context.SaveChangesAsync();
            ViewData["UnconfirmedEmail"] = user.Email;
            ModelState.AddModelError(
                string.Empty,
                "Your password is correct, but your email address has not been activated yet.");
            return View(model);
        }

        // Refreshes an older password hash when the framework recommends an upgrade.
        if (needsRehash)
        {
            user.PasswordHash = _passwordHashService.HashPassword(model.Password);
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;
        AddLoginAttempt(user, normalizedEmail, true);
        await _context.SaveChangesAsync();

        // Claims become the signed-in user's identity and role inside the cookie.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ProfileImageClaimType, GetProfileImagePath(user.ProfileImageFileName))
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authenticationProperties);

        // Only local return URLs are accepted to prevent open-redirect attacks.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // Displays the signed-in user's account information.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Challenge();
        }

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImagePath = GetProfileImagePath(user.ProfileImageFileName),
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return View(model);
    }

    // Displays the profile editing form.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileEditViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            CurrentProfileImagePath = GetProfileImagePath(user.ProfileImageFileName)
        });
    }

    // Updates only the editable profile fields; email and role remain protected.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Challenge();
        }

        if (model.ProfileImage is not null)
        {
            await ValidateProfileImageAsync(model.ProfileImage);
        }

        if (!ModelState.IsValid)
        {
            model.CurrentProfileImagePath = GetProfileImagePath(user.ProfileImageFileName);
            return View(model);
        }

        string? newProfileImageFileName = null;

        if (model.ProfileImage is not null)
        {
            try
            {
                newProfileImageFileName = await SaveProfileImageAsync(model.ProfileImage);
            }
            catch (IOException)
            {
                ModelState.AddModelError(
                    nameof(model.ProfileImage),
                    "The profile image could not be saved. Try another image.");

                model.CurrentProfileImagePath = GetProfileImagePath(user.ProfileImageFileName);
                return View(model);
            }
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
            ? null
            : model.PhoneNumber.Trim();

        var previousProfileImageFileName = user.ProfileImageFileName;

        if (newProfileImageFileName is not null)
        {
            user.ProfileImageFileName = newProfileImageFileName;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            DeleteProfileImage(newProfileImageFileName);
            ModelState.AddModelError(string.Empty, "The profile could not be updated. Try again.");
            model.CurrentProfileImagePath = GetProfileImagePath(previousProfileImageFileName);
            return View(model);
        }

        if (newProfileImageFileName is not null)
        {
            DeleteProfileImage(previousProfileImageFileName);
        }

        await RefreshAuthenticationCookieAsync(user);

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    // Displays the password change form for an authenticated user.
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    // Verifies the existing password before storing a new password hash.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Challenge();
        }

        var passwordIsValid = _passwordHashService.VerifyPassword(
            user.PasswordHash,
            model.CurrentPassword,
            out _);

        if (!passwordIsValid)
        {
            ModelState.AddModelError(
                nameof(model.CurrentPassword),
                "The current password is incorrect.");

            return View(model);
        }

        user.PasswordHash = _passwordHashService.HashPassword(model.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    // Only an authenticated user can invoke the logout action.
    [Authorize]
    // POST prevents a simple URL visit from changing the user's authentication state.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<EmailVerificationToken?> FindActiveVerificationTokenAsync(
        string? plainToken)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
        {
            return null;
        }

        var tokenHash = HashSecurityToken(plainToken);
        var now = DateTime.UtcNow;

        return await _context.EmailVerificationTokens
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate =>
                candidate.TokenHash == tokenHash &&
                candidate.UsedAt == null &&
                candidate.ExpiresAt > now &&
                candidate.User.IsActive &&
                !candidate.User.EmailConfirmed);
    }

    private async Task SendActivationEmailSafelyAsync(User user, string plainToken)
    {
        var activationUrl = BuildActivationUrl(plainToken);
        if (string.IsNullOrWhiteSpace(activationUrl))
        {
            return;
        }

        try
        {
            await _emailSender.SendAccountActivationAsync(
                user.Email,
                user.FullName,
                activationUrl);
        }
        catch (Exception exception)
            when (exception is SmtpException or
                  InvalidOperationException or
                  IOException)
        {
            // The account remains unverified and the Resend page can retry later.
            _logger.LogError(
                exception,
                "Unable to deliver an activation email for user {UserId}.",
                user.UserId);
        }
    }

    // Uses Visual Studio Dev Tunnel so activation links also work on a phone.
    private string? BuildActivationUrl(string plainToken)
    {
        var relativeUrl = Url.Action(
            nameof(ActivateEmail),
            "Account",
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
            nameof(ActivateEmail),
            "Account",
            new { token = plainToken },
            Request.Scheme);
    }

    private static string HashSecurityToken(string plainToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
    }

    // Queues a login audit record that SaveChangesAsync() will persist.
    private void AddLoginAttempt(
        // A nonexistent email can still be audited even though its UserId is null.
        User? user,
        string email,
        bool isSuccessful,
        string? failureReason = null)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        _context.LoginAttempts.Add(new LoginAttempt
        {
            // Links the attempt to a specific account when that account exists.
            UserId = user?.UserId,
            // Preserves the normalized email submitted by the visitor.
            EmailAttempted = email,
            // Distinguishes successful authentication from a failed attempt.
            IsSuccessful = isSuccessful,
            // Uses UTC so security records have a consistent time reference.
            AttemptedAt = DateTime.UtcNow,
            // Records the network source of the request.
            IpAddress = Truncate(ipAddress, 45),
            // Records browser and device information supplied by the request.
            UserAgent = Truncate(userAgent, 500),
            // Stores the internal failure reason for auditing, not for public display.
            FailureReason = Truncate(failureReason, 100)
        });
    }

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maximumLength)
        {
            return value;
        }

        // Keeps only the maximum number of characters accepted by the database field.
        return value[..maximumLength];
    }

    // Gives the legitimate user a useful countdown without exposing passwords.
    private static string BuildLockoutMessage(DateTime lockoutEndUtc)
    {
        var remaining = lockoutEndUtc - DateTime.UtcNow;
        var remainingMinutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
        var malaysiaEnd = MalaysiaClock.FromUtc(lockoutEndUtc);

        return
            $"Your account is temporarily locked. Try again in about {remainingMinutes} " +
            $"minute(s), after {malaysiaEnd:dd MMM yyyy h:mm tt} (Malaysia time).";
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _context.Users.SingleOrDefaultAsync(user => user.UserId == userId);
    }

    // Reissues the cookie so an edited name appears immediately in the navigation bar.
    private async Task RefreshAuthenticationCookieAsync(User user)
    {
        var authenticationResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ProfileImageClaimType, GetProfileImagePath(user.ProfileImageFileName))
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authenticationResult.Properties ?? new AuthenticationProperties());
    }

    private async Task ValidateProfileImageAsync(IFormFile profileImage)
    {
        if (profileImage.Length == 0 || profileImage.Length > MaxProfileImageBytes)
        {
            ModelState.AddModelError(
                nameof(ProfileEditViewModel.ProfileImage),
                "Select an image smaller than 2 MB.");
            return;
        }

        var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();

        if (!AllowedProfileImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(ProfileEditViewModel.ProfileImage),
                "Only JPG, PNG and WebP images are allowed.");
            return;
        }

        var signature = new byte[12];
        await using var stream = profileImage.OpenReadStream();
        var bytesRead = await stream.ReadAsync(signature.AsMemory(0, signature.Length));

        if (!HasSupportedImageSignature(signature, bytesRead))
        {
            ModelState.AddModelError(
                nameof(ProfileEditViewModel.ProfileImage),
                "The selected file is not a valid JPG, PNG or WebP image.");
        }
    }

    private async Task<string> SaveProfileImageAsync(IFormFile profileImage)
    {
        var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var uploadDirectory = Path.Combine(GetWebRootPath(), "uploads", "profiles");
        Directory.CreateDirectory(uploadDirectory);

        var filePath = Path.Combine(uploadDirectory, fileName);
        await using var outputStream = System.IO.File.Create(filePath);
        await profileImage.CopyToAsync(outputStream);
        return fileName;
    }

    private void DeleteProfileImage(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(GetWebRootPath(), "uploads", "profiles", safeFileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private string GetWebRootPath()
    {
        return _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
    }

    private static string GetProfileImagePath(string? fileName)
    {
        return string.IsNullOrWhiteSpace(fileName)
            ? "/images/default-avatar.svg"
            : $"/uploads/profiles/{Path.GetFileName(fileName)}";
    }

    private static bool HasSupportedImageSignature(byte[] signature, int bytesRead)
    {
        var isJpeg = bytesRead >= 3 &&
                     signature[0] == 0xFF &&
                     signature[1] == 0xD8 &&
                     signature[2] == 0xFF;

        var isPng = bytesRead >= 8 &&
                    signature.AsSpan(0, 8).SequenceEqual(
                        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var isWebP = bytesRead >= 12 &&
                     signature.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                     signature.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        return isJpeg || isPng || isWebP;
    }
}
