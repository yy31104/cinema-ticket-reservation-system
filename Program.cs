using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;
using MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    if (IsReactAppRoute(context.Request))
    {
        await SendReactAppAsync(context);
        return;
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

await AdminRoleService.EnsureAdminRoleAsync(app.Services);

app.Run();

static bool IsApiRequest(HttpRequest request)
{
    return request.Path.StartsWithSegments("/api");
}

static bool IsReactAppRoute(HttpRequest request)
{
    if (!HttpMethods.IsGet(request.Method))
    {
        return false;
    }

    var path = request.Path.Value ?? string.Empty;
    var exactReactRoutes = new[]
    {
        "/login",
        "/register",
        "/screenings",
        "/profile",
        "/admin/users",
        "/admin/screenings/create"
    };

    if (exactReactRoutes.Any(route => string.Equals(path, route, StringComparison.Ordinal)))
    {
        return true;
    }

    const string screeningDetailsPrefix = "/screenings/";
    if (path.StartsWith(screeningDetailsPrefix, StringComparison.Ordinal)
        && int.TryParse(path.AsSpan(screeningDetailsPrefix.Length), out _))
    {
        return true;
    }

    const string adminUserEditPrefix = "/admin/users/";
    return path.StartsWith(adminUserEditPrefix, StringComparison.Ordinal)
        && path.Length > adminUserEditPrefix.Length
        && !path.AsSpan(adminUserEditPrefix.Length).Contains('/');
}

static async Task SendReactAppAsync(HttpContext context)
{
    var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    var indexPath = Path.Combine(webRootPath, "app", "index.html");

    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("React app build not found. Run npm run build in ClientApp.");
        return;
    }

    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(indexPath);
}

public partial class Program
{
}
