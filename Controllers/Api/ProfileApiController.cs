using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;
using MVC.Models.Api;

namespace MVC.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileApiController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileApiController(
        ApplicationDbContext dbContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return Unauthorized(new ApiErrorDto { Message = "User account is no longer available." });
        }

        return Ok(ToProfileDto(user));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto? request)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorDto { Message = "RowVersion is required." });
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ApiErrorDto { Message = "Authentication is required." });
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return Unauthorized(new ApiErrorDto { Message = "User account is no longer available." });
        }

        if (!TryDecodeRowVersion(request.RowVersion, out var originalRowVersion, out var error))
        {
            return BadRequest(new ApiErrorDto { Message = error });
        }

        user.Name = NormalizeOptionalText(request.Name);
        user.Surname = NormalizeOptionalText(request.Surname);
        user.PhoneNumber = NormalizeOptionalText(request.PhoneNumber);

        _dbContext.Entry(user).Property(u => u.RowVersion).OriginalValue = originalRowVersion;

        try
        {
            await _dbContext.SaveChangesAsync();
            return Ok(ToProfileDto(user));
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (currentUser is null)
            {
                await _signInManager.SignOutAsync();
                return Conflict(new ApiErrorDto { Message = "Your account was deleted by another operation." });
            }

            return Conflict(new
            {
                message = "Your profile was modified by another operation. Reload the latest values and try again.",
                current = ToProfileDto(currentUser)
            });
        }
    }

    private static ProfileDto ToProfileDto(ApplicationUser user)
    {
        return new ProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            RowVersion = Convert.ToBase64String(user.RowVersion ?? Array.Empty<byte>())
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryDecodeRowVersion(string? rowVersion, out byte[] value, out string error)
    {
        if (rowVersion is null)
        {
            value = Array.Empty<byte>();
            error = "RowVersion is required.";
            return false;
        }

        try
        {
            value = Convert.FromBase64String(rowVersion);
            error = string.Empty;
            return true;
        }
        catch (FormatException)
        {
            value = Array.Empty<byte>();
            error = "Invalid RowVersion.";
            return false;
        }
    }
}
