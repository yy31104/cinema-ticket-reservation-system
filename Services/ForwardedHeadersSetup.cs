using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace MVC.Services;

/// <summary>
/// Trust X-Forwarded-For / X-Forwarded-Proto only from proxies listed in
/// configuration. Without a list, ASP.NET Core's defaults apply: only loopback
/// (a proxy on the same host) is trusted, so a client talking to the app
/// directly cannot pick its own rate-limit partition by sending the header.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string SectionName = "ForwardedHeaders";

    public static IServiceCollection AddCinemaForwardedHeaders(this IServiceCollection services)
    {
        // Read when the options are first resolved, not at registration, so
        // configuration added later (environment, test hosts) is honoured.
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, config) =>
        {
            var section = config.GetSection(SectionName);
            var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
            var networks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // One hop: the right-most X-Forwarded-For entry, which the trusted
            // proxy appended. Earlier entries are client-supplied.
            options.ForwardLimit = 1;

            if (proxies.Length == 0 && networks.Length == 0)
            {
                return;
            }

            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy.Trim()));
            }

            foreach (var network in networks)
            {
                options.KnownNetworks.Add(ParseNetwork(network));
            }
        });

        return services;
    }

    private static IPNetwork ParseNetwork(string cidr)
    {
        var parts = cidr.Trim().Split('/', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var prefixLength))
        {
            throw new FormatException($"{SectionName}:KnownNetworks entry '{cidr}' must be CIDR, e.g. 10.0.0.0/8.");
        }

        return new IPNetwork(IPAddress.Parse(parts[0]), prefixLength);
    }
}
