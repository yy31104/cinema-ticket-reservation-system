using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;

namespace MVC.Services;

public static class AdminRoleService
{
    public const string AdminRoleName = "Admin";

    /// <summary>Configuration key holding the emails of accounts to promote at startup.</summary>
    public const string AdminEmailsKey = "Admin:Emails";

    /// <summary>
    /// Creates the Admin role and promotes the existing accounts listed under
    /// <c>Admin:Emails</c>. Nothing is promoted implicitly: there are no
    /// built-in email addresses and no "first user by ID" fallback. Accounts
    /// must already exist; listing an email does not reserve it for whoever
    /// registers it later.
    /// </summary>
    public static async Task EnsureAdminRoleAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminRoleService));

        await EnsureRoleExistsAsync(roleManager);

        var configuredEmails = configuration.GetSection(AdminEmailsKey).Get<string[]>() ?? [];
        foreach (var email in configuredEmails.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()))
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                logger.LogWarning("Configured admin {Email} has no account yet; register it, then restart.", email);
                continue;
            }

            if (!await userManager.IsInRoleAsync(user, AdminRoleName))
            {
                await userManager.AddToRoleAsync(user, AdminRoleName);
            }
        }
    }

    /// <summary>
    /// Grants Admin to <paramref name="user"/> only if no admin exists yet.
    /// The read ("is there an admin?") and the write ("add this user") run in one
    /// serializable transaction. On SQLite that is BEGIN IMMEDIATE, which takes the
    /// single write lock before the read, so two first registrations cannot both
    /// observe zero admins and both be promoted.
    /// </summary>
    public static async Task EnsureFirstUserIsAdminAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationUser user)
    {
        await EnsureRoleExistsAsync(roleManager);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existingAdmins = await userManager.GetUsersInRoleAsync(AdminRoleName);
        if (existingAdmins.Count == 0)
        {
            var result = await userManager.AddToRoleAsync(user, AdminRoleName);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return;
            }
        }

        await transaction.CommitAsync();
    }

    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRoleName));
        }
    }
}
