using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SecondHandShop.Infrastructure.Persistence;

internal static class PostgresConnectionStringResolver
{
    private const string MissingConnectionStringMessage =
        "PostgreSQL connection string is not configured. Set ConnectionStrings:DefaultConnection, DATABASE_URL, or SUPABASE_DB_URL.";

    public static string Resolve(IConfiguration configuration)
    {
        return Resolve(configuration, "DefaultConnection");
    }

    public static string Resolve(IConfiguration configuration, string connectionName)
    {
        var rawValue = configuration.GetConnectionString(connectionName)
            ?? configuration["DATABASE_URL"]
            ?? configuration["SUPABASE_DB_URL"];

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException(MissingConnectionStringMessage);
        }

        return Normalize(rawValue);
    }

    public static string Normalize(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException(MissingConnectionStringMessage);
        }

        var trimmed = rawValue.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var directBuilder = new NpgsqlConnectionStringBuilder(trimmed);
            NormalizeHost(directBuilder);
            ApplySupabasePoolerDefaults(directBuilder);
            ApplyLocalhostDefaults(directBuilder);
            return directBuilder.ConnectionString;
        }

        var uri = new Uri(trimmed);
        var userInfoParts = uri.UserInfo.Split(':', 2);
        if (userInfoParts.Length != 2)
        {
            throw new InvalidOperationException("PostgreSQL URI must include both username and password.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfoParts[0]),
            Password = Uri.UnescapeDataString(userInfoParts[1])
        };

        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException("PostgreSQL URI must include a database name.");
        }

        ApplyQueryString(uri.Query, builder);
        NormalizeHost(builder);
        ApplySupabasePoolerDefaults(builder);
        ApplyLocalhostDefaults(builder);
        return builder.ConnectionString;
    }

    private static void ApplyQueryString(string queryString, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return;
        }

        foreach (var segment in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            builder[key] = value;
        }
    }

    private static void NormalizeHost(NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            return;
        }

        var host = builder.Host.Trim();
        if (!host.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var normalized = host["tcp://".Length..];
        if (!Uri.TryCreate($"tcp://{normalized}", UriKind.Absolute, out var uri))
        {
            builder.Host = normalized;
            return;
        }

        builder.Host = uri.Host;
        if (builder.Port == 5432 && !uri.IsDefaultPort)
        {
            builder.Port = uri.Port;
        }
    }

    private static readonly string[] LocalhostAliases =
    [
        "localhost",
        "127.0.0.1",
        "::1",
        "0.0.0.0"
    ];

    // Npgsql 10.0.x has an unresolved race in NpgsqlConnector.ResetCancellation() that throws
    // ObjectDisposedException on a disposed ManualResetEventSlim when a pooled connector is
    // reused after a prior request's cancellation callbacks unwound in the wrong order
    // (npgsql/npgsql#6415, npgsql/efcore.pg#3699). The pool ends up handing out a broken
    // connector on the next SaveChanges. For loopback connections the cost of a fresh
    // connection per request is negligible, so we sidestep the pool entirely there and keep
    // the app usable until an upstream fix ships.
    private static void ApplyLocalhostDefaults(NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            return;
        }

        var host = builder.Host.Trim();
        if (!LocalhostAliases.Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        builder.Pooling = false;
    }

    private static void ApplySupabasePoolerDefaults(NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            return;
        }

        if (!builder.Host.EndsWith(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Supabase already provides server-side pooling. Disabling Npgsql's local pool avoids
        // layering two pools and sidesteps intermittent connector-reset failures during design-time
        // migrations and short-lived CLI processes.
        builder.Pooling = false;

        // Supavisor (Supabase's session pooler) does not support GSS session encryption and will
        // immediately shut down the connection when Npgsql attempts to negotiate it. Npgsql then
        // marks the host offline and clears the pool, disposing a ManualResetEventSlim that the
        // in-flight connector still references — the next ResetCancellation() call throws
        // ObjectDisposedException. Explicitly disabling GSS negotiation removes the trigger.
        // Upstream: npgsql/npgsql#6415.
        builder.GssEncryptionMode = GssEncryptionMode.Disable;
    }
}
