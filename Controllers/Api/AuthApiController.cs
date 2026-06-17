using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using MVC.Models.Api;
using MVC.Services;

namespace MVC.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name?.Trim(),
            Surname = request.Surname?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorDto
            {
                Message = "Registration failed.",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        await AdminRoleService.EnsureFirstUserIsAdminAsync(_userManager, _roleManager, user);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return CreatedAtAction(nameof(Me), await ToAuthUserDtoAsync(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                await _signInManager.SignOutAsync();
                return Unauthorized(new ApiErrorDto { Message = "User account is no longer available." });
            }

            return Ok(await ToAuthUserDtoAsync(user));
        }

        if (result.IsLockedOut || result.RequiresTwoFactor || result.IsNotAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorDto { Message = "Login is not allowed for this account." });
        }

        return Unauthorized(new ApiErrorDto { Message = "Invalid email or password." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new ApiErrorDto { Message = "User account is no longer available." });
        }

        return Ok(await ToAuthUserDtoAsync(user));
    }

    private async Task<AuthUserDto> ToAuthUserDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            RowVersion = Convert.ToBase64String(user.RowVersion ?? Array.Empty<byte>()),
            IsAdmin = roles.Contains("Admin"),
            Roles = roles.ToList()
        };
    }
}
