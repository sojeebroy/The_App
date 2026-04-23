using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using The_App.Models;
using The_App.Services.Interfaces;
using static The_App.Models.User;

namespace The_App.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IEmailService emailService,
        IConfiguration config,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "This email address is already registered.");
            return View(model);
        }

        var token = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            Name = model.Name,
            CompanyName = model.CompanyName,
            CompanyDesignation = model.CompanyDesignation,
            RegisteredAt = DateTime.UtcNow,
            EmailVerificationToken = token,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var verificationLink = $"{baseUrl}/Account/VerifyEmail?userId={user.Id}&token={token}";

        _ = Task.Run(() => _emailService.SendVerificationEmailAsync(user.Email!, user.Name, verificationLink));

        TempData["SuccessMessage"] = $"Welcome, {user.Name}! You are now registered and logged in. " +
            "A verification email has been sent to your address. You can use the app right away.";

        return RedirectToAction("Index", "Users");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (user.Status == UserStatus.Blocked)
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked. Please contact an administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "You have been logged out.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(string userId, string token)
    {
        _logger.LogInformation("VerifyEmail called - UserId: {UserId}, Token: {Token}", userId, token);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("VerifyEmail: Missing userId or token");
            TempData["ErrorMessage"] = "Invalid verification link.";
            return RedirectToAction("Login");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("VerifyEmail: User not found for userId {UserId}", userId);
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login");
        }

        _logger.LogInformation("VerifyEmail: Found user {Email}, Stored Token: {StoredToken}, Received Token: {ReceivedToken}", 
            user.Email, user.EmailVerificationToken ?? "NULL", token);

        var storedToken = user.EmailVerificationToken;
        var decodedToken = Uri.UnescapeDataString(token);

        if (string.IsNullOrEmpty(storedToken))
        {
            _logger.LogWarning("VerifyEmail: No token stored for user {Email}", user.Email);
            TempData["ErrorMessage"] = "This email has already been verified.";
            return RedirectToAction("Index", "Users");
        }

        bool tokenMatches = (storedToken == token || storedToken == decodedToken);
        _logger.LogInformation("VerifyEmail: Token match result: {TokenMatches}", tokenMatches);

        if (!tokenMatches)
        {
            _logger.LogWarning("VerifyEmail: Token mismatch for user {Email}. Stored: {StoredToken}, Received: {ReceivedToken}, Decoded: {DecodedToken}", 
                user.Email, storedToken, token, decodedToken);
            TempData["ErrorMessage"] = "Invalid or expired verification token.";
            return RedirectToAction("Login");
        }

        user.EmailConfirmed = true;
        user.EmailVerificationToken = null;

        if (user.Status == UserStatus.Unverified)
            user.Status = UserStatus.Active;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogError("VerifyEmail: Failed to update user - Errors: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
            TempData["ErrorMessage"] = "Failed to verify email. Please try again.";
            return RedirectToAction("Login");
        }

        _logger.LogInformation("✓ VerifyEmail: Email verified successfully for user {Email}, Status updated to {Status}", 
            user.Email, user.Status);

        TempData["SuccessMessage"] = $"✓ Email verified successfully! Your account status is now Active.";

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Users");
        }

        TempData["SuccessMessage"] = $"✓ Email verified successfully! Please log in with your credentials.";
        return RedirectToAction("Login");
    }
}
