using Microsoft.AspNetCore.Http;
using SecondHandShop.WebApi.Controllers;

namespace SecondHandShop.WebApi.Authentication;

public static class AdminAuthCookies
{
    public static void AppendAuthTokenCookie(
        HttpResponse response,
        string token,
        DateTimeOffset expiresAt,
        AdminAuthCookieOptions options)
    {
        response.Cookies.Append(
            AdminAuthController.CookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = options.Secure,
                SameSite = options.ResolveSameSite(),
                Path = options.Path,
                Expires = expiresAt
            });
    }

    public static void DeleteAuthTokenCookie(HttpResponse response, AdminAuthCookieOptions options)
    {
        response.Cookies.Delete(
            AdminAuthController.CookieName,
            new CookieOptions
            {
                Path = options.Path,
                Secure = options.Secure,
                SameSite = options.ResolveSameSite()
            });
    }

    public const string SessionExpiresHeaderName = "X-Admin-Session-Expires-At";

    public static void AppendSessionExpiresHeader(HttpResponse response, DateTimeOffset expiresAtUtc) =>
        response.Headers.Append(SessionExpiresHeaderName, expiresAtUtc.ToString("o"));
}
