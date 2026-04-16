using Microsoft.AspNetCore.Http;

namespace SecondHandShop.WebApi.Common;

/// <summary>
/// Provides a single, consistent way to resolve the client's real IP address
/// across the entire application (rate limiting, logging, anti-spam, etc.).
///
/// Priority order:
///   1. CF-Connecting-IP  – Cloudflare terminates TLS and writes this header
///      with the *true* client IP before forwarding to the origin. Because
///      Cloudflare is the outermost edge proxy and the only entry-point to our
///      origin (Railway), this header is the most trustworthy source: it cannot
///      be spoofed by the client as Cloudflare always overwrites it.
///   2. X-Forwarded-For   – Standard proxy header. When CF-Connecting-IP is
///      absent (e.g. local dev, direct access), we fall back to the leftmost
///      (first) entry, which represents the originating client per RFC 7239.
///   3. RemoteIpAddress   – The TCP-level peer address. Accurate only when
///      there is no reverse proxy in front of Kestrel.
///   4. "unknown"         – Defensive fallback so callers never deal with null.
/// </summary>
public static class IpHelper
{
    private const string CfConnectingIpHeader = "CF-Connecting-IP";
    private const string XForwardedForHeader = "X-Forwarded-For";
    private const string UnknownIp = "unknown";

    /// <summary>
    /// Resolves the real client IP from the current HTTP context.
    /// Safe to call from controllers, middleware, and rate-limit partition functions.
    /// </summary>
    public static string GetClientIp(HttpContext context)
    {
        // 1. Cloudflare sets this header on every proxied request.
        //    It always contains a single IP and is never a comma-separated list.
        var cfIp = GetHeaderValue(context, CfConnectingIpHeader);
        if (cfIp is not null)
            return cfIp;

        // 2. Standard proxy chain header – take the first (leftmost) entry,
        //    which is the original client IP added by the outermost proxy.
        var xffIp = GetFirstForwardedFor(context);
        if (xffIp is not null)
            return xffIp;

        // 3. Direct TCP peer address (no proxy scenario / local development).
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is not null)
        {
            // Normalise IPv4-mapped IPv6 (e.g. ::ffff:127.0.0.1 → 127.0.0.1)
            // so that rate-limit partition keys and log entries are consistent.
            return remoteIp.IsIPv4MappedToIPv6
                ? remoteIp.MapToIPv4().ToString()
                : remoteIp.ToString();
        }

        // 4. Should never happen in production, but stay null-safe.
        return UnknownIp;
    }

    /// <summary>
    /// Reads a single-value header, trims whitespace, and returns null if empty.
    /// </summary>
    private static string? GetHeaderValue(HttpContext context, string headerName)
    {
        var value = context.Request.Headers[headerName].ToString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > 0 ? trimmed : null;
    }

    /// <summary>
    /// Extracts the first IP from the X-Forwarded-For header.
    /// The header may contain "client, proxy1, proxy2" — we want only "client".
    /// </summary>
    private static string? GetFirstForwardedFor(HttpContext context)
    {
        var value = context.Request.Headers[XForwardedForHeader].ToString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Take everything before the first comma (or the whole string if no comma).
        var commaIndex = value.IndexOf(',');
        var firstEntry = commaIndex >= 0
            ? value[..commaIndex]
            : value;

        var trimmed = firstEntry.Trim();
        return trimmed.Length > 0 ? trimmed : null;
    }
}
