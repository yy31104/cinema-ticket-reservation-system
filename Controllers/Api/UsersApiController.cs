using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;
using MVC.Models.Api;

namespace MVC.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersApiController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersApiController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                Name = u.Name,
                Surname = u.Surname,
                PhoneNumber = u.PhoneNumber,
                RowVersion = Convert.ToBase64String(u.RowVersion ?? Array.Empty<byte>())
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return NotFound(new ApiErrorDto { Message = "User not found." });
        }

        return Ok(ToUserDetailsDto(user));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequestDto? request)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorDto { Message = "RowVersion is required." });
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound(new ApiErrorDto { Message = "User not found." });
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
            return Ok(ToUserDetailsDto(user));
        }
        catch (DbUpdateConcurrencyException)
        {
            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == id);

            if (existingUser is null)
            {
                return Conflict(new ApiErrorDto { Message = "User was deleted by another operation." });
            }

            return Conflict(new
            {
                message = "User was modified by another operation. Reload the latest values and try again.",
                current = ToUserDetailsDto(existingUser)
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id, [FromQuery] DeleteUserRequestDto request)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorDto { Message = "You cannot delete your own account." });
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound(new ApiErrorDto { Message = "User not found." });
        }

        var submittedRowVersion = request.RowVersion;
        if (submittedRowVersion is null && Request.Query.ContainsKey("rowVersion"))
        {
            submittedRowVersion = Request.Query["rowVersion"].ToString();
        }

        if (!TryDecodeRowVersion(submittedRowVersion, out var originalRowVersion, out var error))
        {
            return BadRequest(new ApiErrorDto { Message = error });
        }

        _dbContext.Entry(user).Property(u => u.RowVersion).OriginalValue = originalRowVersion;
        _dbContext.Users.Remove(user);

        try
        {
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "User deleted successfully." });
        }
        catch (DbUpdateConcurrencyException)
        {
            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == id);

            if (existingUser is null)
            {
                return Conflict(new ApiErrorDto { Message = "User was already deleted by another operation." });
            }

            return Conflict(new
            {
                message = "User was modified by another operation. Reload the latest values and try again.",
                current = ToUserDetailsDto(existingUser)
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new ApiErrorDto { Message = "User cannot be deleted because related records still exist." });
        }
    }

    private static UserDetailsDto ToUserDetailsDto(ApplicationUser user)
    {
        return new UserDetailsDto
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
