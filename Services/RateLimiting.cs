using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MVC.Models.Api;

namespace MVC.Services;

public sealed class FixedWindowLimit
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Login attempts (POST only) per client IP.</summary>
    public FixedWindowLimit Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Reservation requests per signed-in user, shared by the MVC form and the API.</summary>
    public FixedWindowLimit Reservations { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };
}

/// <summary>
/// Named policies for ASP.NET Core's rate limiter. Endpoints opt in with
/// [EnableRateLimiting(RateLimitPolicies.X)]; everything else is unlimited.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Reservations = "reservations";

    public static IServiceCollection AddCinemaRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(Login, context =>
            {
                // Rendering the login form is free; only credential submissions count.
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    return RateLimitPartition.GetNoLimiter("login:read");
                }

                return FixedWindow($"login:ip:{ClientIp(context)}", LimitsFor(context).Login);
            });

            options.AddPolicy(Reservations, context =>
            {
                // Signed-in users get their own bucket so people behind one NAT do
                // not block each other; anonymous calls (rejected later by
                // [Authorize]) fall back to the client IP.
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var key = string.IsNullOrEmpty(userId)
                    ? $"reservations:ip:{ClientIp(context)}"
                    : $"reservations:user:{userId}";

                return FixedWindow(key, LimitsFor(context).Reservations);
            });
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(string key, FixedWindowLimit limit)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit.PermitLimit,
            Window = TimeSpan.FromSeconds(limit.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    private static RateLimitingOptions LimitsFor(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
    }

    /// <summary>
    /// Uses the socket address. Behind a reverse proxy, configure
    /// ForwardedHeadersMiddleware first or every client shares the proxy's IP.
    /// </summary>
    private static string ClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        var response = rejected.HttpContext.Response;

        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        const string message = "Too many requests. Please wait a moment and try again.";
        if (rejected.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            await response.WriteAsJsonAsync(new ApiErrorDto { Message = message }, cancellationToken);
        }
        else
        {
            response.ContentType = "text/plain; charset=utf-8";
            await response.WriteAsync(message, cancellationToken);
        }
    }
}
