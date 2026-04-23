using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using The_App.Data;
using The_App.Models;
using static The_App.Models.User;

namespace The_App.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ApplicationDbContext db,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.LastLoginAt)
            .ThenByDescending(u => u.RegisteredAt)
            .Select(u => new User
            {
                Id = u.Id,
                Name = u.Name,
                CompanyName = u.CompanyName ?? string.Empty,
                CompanyDesignation = u.CompanyDesignation ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email ?? string.Empty,
                Status = u.Status,
                LastLoginAt = u.LastLoginAt,
                RegisteredAt = u.RegisteredAt,
                EmailConfirmed = u.EmailConfirmed
            })
            .ToListAsync();

        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block([FromBody] BulkActionRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return Json(new { success = false, message = "No users selected." });

        var currentUserId = _userManager.GetUserId(User);

        var users = await _db.Users
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
            user.Status = UserStatus.Blocked;

        await _db.SaveChangesAsync();

        if (request.Ids.Contains(currentUserId!))
        {
            await _signInManager.SignOutAsync();
            return Json(new { success = true, selfAffected = true, redirectUrl = "/Account/Login", message = "Your account has been blocked." });
        }

        return Json(new { success = true, message = $"{users.Count} user(s) blocked." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock([FromBody] BulkActionRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return Json(new { success = false, message = "No users selected." });

        var users = await _db.Users
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            if (user.Status == UserStatus.Blocked)
                user.Status = user.EmailConfirmed ? UserStatus.Active : UserStatus.Unverified;
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = $"{users.Count} user(s) unblocked." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] BulkActionRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return Json(new { success = false, message = "No users selected." });

        var currentUserId = _userManager.GetUserId(User);
        bool selfDeleted = request.Ids.Contains(currentUserId!);

        var users = await _db.Users
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync();

        int count = users.Count;

        if (selfDeleted)
            await _signInManager.SignOutAsync();

        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        if (selfDeleted)
            return Json(new { success = true, selfAffected = true, redirectUrl = "/Account/Login", message = "Your account has been deleted." });

        return Json(new { success = true, message = $"{count} user(s) deleted." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnverified([FromBody] BulkActionRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return Json(new { success = false, message = "No users selected." });

        var currentUserId = _userManager.GetUserId(User);

        var users = await _db.Users
            .Where(u => request.Ids.Contains(u.Id) && (u.Status == UserStatus.Unverified || !u.EmailConfirmed))
            .ToListAsync();

        if (users.Count == 0)
        {
            _logger?.LogInformation("DeleteUnverified: no unverified users found in selection (ids={Ids})", string.Join(',', request.Ids));
            return Json(new { success = false, message = "No unverified users found in selection." });
        }

        bool selfDeleted = users.Any(u => u.Id == currentUserId);

        if (selfDeleted)
            await _signInManager.SignOutAsync();

        int count = users.Count;
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        if (selfDeleted)
            return Json(new { success = true, selfAffected = true, redirectUrl = "/Account/Login", message = "Your account has been deleted." });

        return Json(new { success = true, message = $"{count} unverified user(s) deleted." });
    }
}

public class BulkActionRequest
{
    public List<string> Ids { get; set; } = new();
}
