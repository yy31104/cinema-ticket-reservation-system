using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVC.Data;
using MVC.Models;
using MVC.Models.Api;
using MVC.Services;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

public sealed class AdminIdentityTests : ApiTestBase
{
    [Fact]
    public async Task FormerlyHardCodedEmail_IsNotPromotedAfterRestart()
    {
        await AuthTestHelpers.RegisterAdminAsync(Factory);
        var (_, user) = await AuthTestHelpers.RegisterWithEmailAsync(Factory, "user@example.com");
        Assert.False(user.IsAdmin);

        // A new host over the same database runs the startup bootstrap again.
        using var restarted = Factory.WithWebHostBuilder(_ => { });
        restarted.CreateClient().Dispose();

        Assert.False(await IsAdminAsync(restarted.Services, "user@example.com"));
    }

    [Fact]
    public async Task ConfiguredAdminEmail_IsPromotedAtStartup()
    {
        await AuthTestHelpers.RegisterAdminAsync(Factory);
        const string email = "operator@example.test";
        var (_, user) = await AuthTestHelpers.RegisterWithEmailAsync(Factory, email);
        Assert.False(user.IsAdmin);

        using var restarted = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting($"{AdminRoleService.AdminEmailsKey}:0", email));
        restarted.CreateClient().Dispose();

        Assert.True(await IsAdminAsync(restarted.Services, email));
    }

    [Fact]
    public async Task FirstAdminGrant_PromotesExactlyOneOfConcurrentFirstUsers()
    {
        // A file database gives each scope its own connection, so the two grants
        // really race. Without a transaction both can read "no admins yet".
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var path = Path.Combine(Path.GetTempPath(), $"cinema-admin-race-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildIdentityServices(path);
                var users = new List<ApplicationUser>();
                using (var scope = provider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await context.Database.EnsureCreatedAsync();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    for (var i = 0; i < 2; i++)
                    {
                        var user = new ApplicationUser { UserName = $"first{i}@example.test", Email = $"first{i}@example.test" };
                        Assert.True((await userManager.CreateAsync(user, AuthTestHelpers.Password)).Succeeded);
                        users.Add(user);
                    }

                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    await roleManager.CreateAsync(new IdentityRole(AdminRoleService.AdminRoleName));
                }

                using var start = new ManualResetEventSlim(false);
                var grants = users.Select(user => Task.Run(async () =>
                {
                    using var scope = provider.CreateScope();
                    var services = scope.ServiceProvider;
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var own = await userManager.FindByIdAsync(user.Id);
                    start.Wait();
                    await AdminRoleService.EnsureFirstUserIsAdminAsync(
                        services.GetRequiredService<ApplicationDbContext>(),
                        userManager,
                        services.GetRequiredService<RoleManager<IdentityRole>>(),
                        own!);
                })).ToArray();
                start.Set();
                await Task.WhenAll(grants);

                using var check = provider.CreateScope();
                var admins = await check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
                    .GetUsersInRoleAsync(AdminRoleService.AdminRoleName);
                Assert.Single(admins);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task RepeatedFailedLogins_LockTheAccount()
    {
        await AuthTestHelpers.RegisterAdminAsync(Factory);
        const string email = "lockout@example.test";
        await AuthTestHelpers.RegisterWithEmailAsync(Factory, email);
        var client = Factory.CreateApiClient();

        // MaxFailedAccessAttempts is 5: four failures are plain 401s, and the
        // fifth failure is the one that locks the account.
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = email,
                Password = "wrong-password"
            });
            Assert.Equal(attempt < 5 ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, failed.StatusCode);
        }

        // While locked, even the correct password is refused.

        var correct = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = AuthTestHelpers.Password
        });

        Assert.Equal(HttpStatusCode.Forbidden, correct.StatusCode);
        var error = await correct.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Contains("temporarily locked", error!.Message);
    }

    private static async Task<bool> IsAdminAsync(IServiceProvider services, string email)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user is not null && await userManager.IsInRoleAsync(user, AdminRoleService.AdminRoleName);
    }

    private static ServiceProvider BuildIdentityServices(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"DataSource={databasePath};Default Timeout=30"));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }
}
