using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MVC.Tests.Infrastructure;

/// <summary>
/// TestServer has no real socket, so RemoteIpAddress is null. A request may
/// send <see cref="HeaderName"/> to stand in for the TCP peer address. This runs
/// before the app's own middleware (including UseForwardedHeaders), exactly
/// where a real socket address would already be set.
/// </summary>
public sealed class SocketAddressStartupFilter : IStartupFilter
{
    public const string HeaderName = "X-Test-Socket-Address";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(HeaderName, out var value))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(value.ToString());
                    context.Request.Headers.Remove(HeaderName);
                }

                return nextMiddleware(context);
            });

            next(app);
        };
    }
}
