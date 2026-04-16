using System.Net;

namespace SecondHandShop.WebApi.Middleware;

/// <summary>
/// Rejects any request that did not arrive through Cloudflare.
///
/// Detection method: Cloudflare always sets the <c>CF-Connecting-IP</c> header
/// on every request it proxies. This header contains the true client IP and is
/// written by Cloudflare itself — it is not forwarded from the client's original
/// request. A direct request to the Railway origin URL will never carry this
/// header (unless an attacker manually adds it, which is addressed below).
///
/// <para><strong>Why this is secure:</strong></para>
/// <list type="bullet">
///   <item>When Cloudflare is the sole entry point, it <em>overwrites</em>
///   <c>CF-Connecting-IP</c> regardless of what the client sends. An attacker
///   who routes through Cloudflare therefore cannot forge a different value.</item>
///   <item>An attacker who bypasses Cloudflare and hits the Railway origin
///   directly <em>can</em> set a fake <c>CF-Connecting-IP</c> header — but this
///   middleware alone is not the full defence. Production deployments should also
///   restrict origin ingress to Cloudflare IP ranges at the network/firewall
///   level (Railway does not currently support IP allowlists, so this middleware
///   provides the application-layer check as defense-in-depth).</item>
/// </list>
///
/// <para>The middleware is <strong>disabled in Development</strong> by default
/// so that local development without Cloudflare works normally. This is
/// controlled by the <c>Security:RequireCloudflare</c> configuration key.</para>
/// </summary>
public sealed class CloudflareOnlyMiddleware(
    RequestDelegate next,
    ILogger<CloudflareOnlyMiddleware> logger,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    private const string CfConnectingIpHeader = "CF-Connecting-IP";

    /// <summary>
    /// Loopback addresses that are always allowed through, so that health checks
    /// and local development are not blocked.
    /// </summary>
    private static readonly HashSet<string> LoopbackAddresses =
        ["127.0.0.1", "::1", "localhost"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldEnforce())
        {
            await next(context);
            return;
        }

        // Allow loopback connections (health checks, container-internal probes).
        if (IsLoopback(context))
        {
            await next(context);
            return;
        }

        var cfHeader = context.Request.Headers[CfConnectingIpHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(cfHeader))
        {
            logger.LogWarning(
                "Blocked direct-to-origin request without {Header} from {RemoteIp}",
                CfConnectingIpHeader,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Direct access to origin is not allowed.");
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Determines whether the middleware should enforce the Cloudflare check.
    /// Disabled in Development unless explicitly opted in via configuration.
    /// </summary>
    private bool ShouldEnforce()
    {
        // Explicit config takes precedence over environment convention.
        var configured = configuration.GetValue<bool?>("Security:RequireCloudflare");
        if (configured.HasValue)
            return configured.Value;

        // Default: enforce in Production/Staging, skip in Development.
        return !environment.IsDevelopment();
    }

    private static bool IsLoopback(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null) return false;

        if (IPAddress.IsLoopback(remoteIp)) return true;

        var remoteStr = remoteIp.IsIPv4MappedToIPv6
            ? remoteIp.MapToIPv4().ToString()
            : remoteIp.ToString();

        return LoopbackAddresses.Contains(remoteStr);
    }
}
