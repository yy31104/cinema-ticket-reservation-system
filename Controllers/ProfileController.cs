using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;

namespace MVC.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            TempData["ErrorMessage"] = "Your account is no longer available.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            TempData["ErrorMessage"] = "Your account was deleted.";
            return RedirectToAction("Index", "Home");
        }

        byte[] originalRowVersion;
        try
        {
            originalRowVersion = string.IsNullOrWhiteSpace(model.RowVersion)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(model.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Invalid profile version. Refresh the page and try again.");
            return View(model);
        }

        user.Name = model.Name?.Trim();
        user.Surname = model.Surname?.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        _dbContext.Entry(user).Property(u => u.RowVersion).OriginalValue = originalRowVersion;

        try
        {
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Edit));
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (currentUser is null)
            {
                await _signInManager.SignOutAsync();
                TempData["ErrorMessage"] = "Your account was deleted by another operation.";
                return RedirectToAction("Index", "Home");
            }

            ModelState.Clear();
            var latest = ToViewModel(currentUser);
            ModelState.AddModelError(string.Empty, "Your profile was modified by someone else. Latest values were loaded.");
            return View(latest);
        }
    }

    private static ProfileEditViewModel ToViewModel(ApplicationUser user)
    {
        return new ProfileEditViewModel
        {
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            RowVersion = Convert.ToBase64String(user.RowVersion ?? Array.Empty<byte>())
        };
    }
}
