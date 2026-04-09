namespace SecondHandShop.WebApi.Authentication;

/// <summary>
/// Binds from the "AdminAuth:Cookie" configuration section. Governs the shape of the HttpOnly
/// session cookie used for admin authentication.
///
/// SameSite guidance:
///   - Same-site deployments (SPA and API share an eTLD+1, e.g. admin.example.com + api.example.com
///     both under example.com, or localhost:5173 + localhost:7266 in development): use "Strict" or
///     "Lax". Strict gives stronger CSRF protection and is the default.
///   - Cross-site deployments (SPA and API on unrelated registrable domains): MUST use "None"
///     together with Secure=true, otherwise the browser will not forward the cookie on cross-site
///     XHR/fetch requests and login will silently fail.
/// </summary>
public sealed class AdminAuthCookieOptions
{
    public const string SectionName = "AdminAuth:Cookie";

    /// <summary>
    /// One of: "Strict", "Lax", "None". Defaults to "Strict".
    /// </summary>
    public string SameSite { get; set; } = "Strict";

    /// <summary>
    /// Whether the cookie is only sent over HTTPS. Must be true in any non-local environment,
    /// and is required when SameSite=None.
    /// </summary>
    public bool Secure { get; set; } = true;

    /// <summary>
    /// Cookie path scope. Defaults to "/api/lord" so the cookie is only sent with admin API calls.
    /// </summary>
    public string Path { get; set; } = "/api/lord";

    public SameSiteMode ResolveSameSite() => SameSite?.Trim().ToLowerInvariant() switch
    {
        "strict" => SameSiteMode.Strict,
        "lax" => SameSiteMode.Lax,
        "none" => SameSiteMode.None,
        _ => throw new InvalidOperationException(
            $"{SectionName}:SameSite must be 'Strict', 'Lax', or 'None' (got '{SameSite}').")
    };

    public void Validate()
    {
        var mode = ResolveSameSite();

        // Browsers reject SameSite=None without Secure, so surface the misconfiguration at startup
        // instead of letting login silently fail in the browser.
        if (mode == SameSiteMode.None && !Secure)
        {
            throw new InvalidOperationException(
                $"{SectionName}: SameSite=None requires Secure=true. " +
                "Either enable Secure (and serve over HTTPS) or switch SameSite to Strict/Lax.");
        }

        if (string.IsNullOrWhiteSpace(Path))
        {
            throw new InvalidOperationException($"{SectionName}:Path is required.");
        }
    }
}
