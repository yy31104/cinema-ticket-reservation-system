using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;

namespace MVC.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new AdminUserListItemViewModel
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                Name = u.Name,
                Surname = u.Surname
            })
            .ToListAsync();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found or already deleted.";
            return RedirectToAction(nameof(Index));
        }

        return View(ToEditViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == model.Id);
        if (user is null)
        {
            TempData["ErrorMessage"] = "User was deleted by another operation.";
            return RedirectToAction(nameof(Index));
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
            ModelState.AddModelError(string.Empty, "Invalid version token. Refresh the page and try again.");
            return View(model);
        }

        user.Name = model.Name?.Trim();
        user.Surname = model.Surname?.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        _dbContext.Entry(user).Property(u => u.RowVersion).OriginalValue = originalRowVersion;

        try
        {
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == model.Id);

            if (existingUser is null)
            {
                TempData["ErrorMessage"] = "User was deleted by another operation.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "User was modified by someone else. Latest values were loaded.");
            return View(ToEditViewModel(existingUser));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found or already deleted.";
            return RedirectToAction(nameof(Index));
        }

        var model = new AdminUserDeleteViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            RowVersion = Convert.ToBase64String(user.RowVersion ?? Array.Empty<byte>())
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(AdminUserDeleteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User);
        if (model.Id == currentUserId)
        {
            ModelState.AddModelError(string.Empty, "You cannot delete your own account.");
            return View(model);
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == model.Id);
        if (user is null)
        {
            TempData["StatusMessage"] = "User was already deleted.";
            return RedirectToAction(nameof(Index));
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
            TempData["ErrorMessage"] = "Invalid version token. Please try deleting again.";
            return RedirectToAction(nameof(Delete), new { id = model.Id });
        }

        _dbContext.Entry(user).Property(u => u.RowVersion).OriginalValue = originalRowVersion;
        _dbContext.Users.Remove(user);

        try
        {
            await _dbContext.SaveChangesAsync();
            TempData["StatusMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == model.Id);

            if (existingUser is null)
            {
                TempData["StatusMessage"] = "User was already deleted by another operation.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "User was modified by someone else. Please review and try again.";
            return RedirectToAction(nameof(Delete), new { id = model.Id });
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "User cannot be deleted right now (for example, they may still have reservations).";
            return RedirectToAction(nameof(Delete), new { id = model.Id });
        }
    }

    private static AdminUserEditViewModel ToEditViewModel(ApplicationUser user)
    {
        return new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            RowVersion = Convert.ToBase64String(user.RowVersion ?? Array.Empty<byte>())
        };
    }
}
