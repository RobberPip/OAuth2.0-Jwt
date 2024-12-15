using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Utils.Helpers;

namespace Utils;

public class TokenMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var accessToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(accessToken) || IsTokenExpired(accessToken))
        {
            var refreshToken = context.Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var newTokens = await RefreshTokensAsync(refreshToken, context);
                if (newTokens != null)
                {
                    context.Request.Headers["Authorization"] = $"Bearer {newTokens.Value.AccessToken}";

                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        Expires = newTokens.Value.Expires
                    };
                    context.Response.Cookies.Append("refreshToken", newTokens.Value.RefreshToken ?? string.Empty, cookieOptions);
                    context.Response.Headers["NewAccessToken"] = newTokens.Value.AccessToken;
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
        } // TODO Выбрасывать занового авторизоваться если не прошёл токен
        await _next(context);
    }
    private bool IsTokenExpired(string accessToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(accessToken);

        return jwtToken.ValidTo < DateTime.UtcNow;
    }
    private async Task<(string? AccessToken, string? RefreshToken, DateTime? Expires)?> RefreshTokensAsync(string refreshToken, HttpContext context)
    {
        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, Links.AuthenticationService)
        {
            Content = new StringContent($"\"{refreshToken}\"", Encoding.UTF8, "application/json")
        };
        var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonDocument.Parse(json);
            string? newAccessToken = tokenData.RootElement.GetProperty("AccessToken").GetString();
            string? newRefreshToken = tokenData.RootElement.GetProperty("RefreshToken").GetString();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(refreshToken);
            var exp = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (long.TryParse(exp, out var expUnix))
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                return (newAccessToken, newRefreshToken, expiresAt);
            }
        }
        return null;
    }
}