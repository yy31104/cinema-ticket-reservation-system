using System.Net;
using System.Net.Http.Json;
using MVC.Models.Api;
using MVC.Services;
using MVC.Tests.Infrastructure;

namespace MVC.Tests.Api;

/// <summary>
/// The login limiter partitions by client IP. These tests check which address
/// that is: the forwarded client behind a trusted proxy, and the real peer
/// otherwise, so a client cannot pick a fresh partition by sending X-Forwarded-For.
/// </summary>
public sealed class ForwardedHeadersTests : ApiTestBase
{
    private const string TrustedProxy = "10.0.0.5";

    [Fact]
    public async Task TrustedProxy_EachForwardedClientGetsItsOwnLoginBudget()
    {
        Factory.Settings["ForwardedHeaders:KnownProxies:0"] = TrustedProxy;
        LimitLoginTo(2);
        var client = CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, TrustedProxy, "203.0.113.10"));
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, TrustedProxy, "203.0.113.10"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, TrustedProxy, "203.0.113.10"));

        // Another client behind the same proxy is not affected.
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, TrustedProxy, "203.0.113.11"));

        // Only the entry the proxy appended (right-most) counts; a value the
        // client prepended does not move it to a fresh partition.
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, TrustedProxy, "198.18.0.1, 203.0.113.10"));
    }

    [Fact]
    public async Task TrustedNetwork_FromCidrConfiguration_IsHonoured()
    {
        Factory.Settings["ForwardedHeaders:KnownNetworks:0"] = "10.1.0.0/16";
        LimitLoginTo(1);
        var client = CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "10.1.2.3", "203.0.113.30"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, "10.1.2.3", "203.0.113.30"));
        // Same proxy, different forwarded client: separate budget.
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "10.1.2.3", "203.0.113.31"));

        // Any address inside the network is trusted, not just the first one seen.
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "10.1.200.9", "203.0.113.32"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, "10.1.200.9", "203.0.113.32"));
    }

    [Fact]
    public async Task UntrustedPeer_CannotDodgeTheLimitBySpoofingForwardedFor()
    {
        LimitLoginTo(2);
        var client = CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "198.51.100.7", "1.1.1.1"));
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "198.51.100.7", "2.2.2.2"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, "198.51.100.7", "3.3.3.3"));
    }

    [Fact]
    public async Task DefaultConfig_TrustsOnlyLoopback()
    {
        LimitLoginTo(1);
        var client = CreateClient();

        // A proxy on the same host (loopback) is trusted out of the box.
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "127.0.0.1", "203.0.113.20"));
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "127.0.0.1", "203.0.113.21"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, "127.0.0.1", "203.0.113.21"));
    }

    [Fact]
    public async Task ConfiguredProxyList_ReplacesTheLoopbackDefault()
    {
        Factory.Settings["ForwardedHeaders:KnownProxies:0"] = TrustedProxy;
        LimitLoginTo(1);
        var client = CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginFrom(client, "127.0.0.1", "203.0.113.40"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom(client, "127.0.0.1", "203.0.113.41"));
    }

    private void LimitLoginTo(int permits)
    {
        Factory.RateLimits = o => o.Login = new FixedWindowLimit { PermitLimit = permits, WindowSeconds = 60 };
    }

    private static async Task<HttpStatusCode> LoginFrom(HttpClient client, string socketAddress, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequestDto { Email = "nobody@example.test", Password = "wrong-password" })
        };
        request.Headers.Add(SocketAddressStartupFilter.HeaderName, socketAddress);
        request.Headers.Add("X-Forwarded-For", forwardedFor);

        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
