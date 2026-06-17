using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MVC.Models;

namespace MVC.Services;

public static class AdminRoleService
{
    private const string AdminRoleName = "Admin";

    private static readonly string[] KnownAdminEmails =
    [
        "admin@example.com",
        "user@example.com"
    ];

    public static async Task EnsureAdminRoleAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleExistsAsync(roleManager);

        foreach (var email in KnownAdminEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null && !await userManager.IsInRoleAsync(user, AdminRoleName))
            {
                await userManager.AddToRoleAsync(user, AdminRoleName);
            }
        }

        var existingAdmins = await userManager.GetUsersInRoleAsync(AdminRoleName);
        if (existingAdmins.Count > 0)
        {
            return;
        }

        var firstUser = await userManager.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
        if (firstUser is not null)
        {
            await userManager.AddToRoleAsync(firstUser, AdminRoleName);
        }
    }

    public static async Task EnsureFirstUserIsAdminAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationUser user)
    {
        await EnsureRoleExistsAsync(roleManager);

        var existingAdmins = await userManager.GetUsersInRoleAsync(AdminRoleName);
        if (existingAdmins.Count == 0)
        {
            await userManager.AddToRoleAsync(user, AdminRoleName);
        }
    }

    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRoleName));
        }
    }
}
