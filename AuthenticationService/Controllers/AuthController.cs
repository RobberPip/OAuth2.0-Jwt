using AuthenticationService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using AuthenticationService.Models;

namespace AuthenticationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }
        [HttpDelete("logout")]
        [SwaggerOperation(
            Summary = "Выход",
            Description = "Этот метод деактивирует сеесию пользователя.")]
        public async Task<IActionResult> Logout()
        {
            string? refreshToken = HttpContext.Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest("Refresh token not found.");
            }            
            // Извлекаем информацию о пользователе из токена
            UserModel? userModel = _authService.ExtractClaimsFromRefreshToken(refreshToken);
            if (userModel != null)
            {
                // Удаляем сессию пользователя
                await _authService.DeleteSessionUser(userModel.Uid, userModel.JwtTokens.RefreshTokenJti);
                // Удаляем куку, устанавливая истекший срок действия
                Response.Cookies.Append("refreshToken", "", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(-1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
                //TODO после логаута нужно чистить access токен на клиенте
                return Ok("Logout successful.");
            }
            return BadRequest();
        }

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Логин пользователя",
            Description = "Этот метод авторизует пользователя, создаёт ему JWT токены и сессию в БД.")]
        public async Task<IActionResult> Login([FromBody] UserModel? modelUser)
        {
            if (modelUser == null || string.IsNullOrWhiteSpace(modelUser.Login) || string.IsNullOrWhiteSpace(modelUser.Password))
            {
                return BadRequest("Invalid login credentials."); 
            }
            UserModel? user = await _authService.AuthenticationUserAsync(modelUser.Login, modelUser.Password);
            if (user != null)
            {
                SetRefreshTokenCookie(user.JwtTokens);
                return Ok(new
                {
                    AccessToken = user.JwtTokens.AccessToken,
                });
            }

            return Unauthorized();
        }
        [HttpPost("refreshToken")]
        [SwaggerOperation(
            Summary = "Обновление refresh токен",
            Description = "Этот метод обновляет refresh токен, соверашая проверку по access токену.")]
        public async Task<IActionResult> Refresh([FromBody] string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest("Invalid refresh token.");
            }
            var tokenClaims = _authService.ExtractClaimsFromRefreshToken(refreshToken);
            if (tokenClaims == null)
            {
                return Unauthorized();
            }
            Guid userUid = tokenClaims.Uid;
            Guid? jti = tokenClaims.JwtTokens.RefreshTokenJti;
            if (await _authService.ValidateRefreshToken(userUid,jti))
            {
                string? newAccessToken = _authService.GenerateAccessToken(userUid.ToString()); 
                var newRefreshToken = _authService.GenerateRefreshToken(userUid.ToString());
                UserModel? userModel = _authService.ExtractClaimsFromRefreshToken(newRefreshToken.Token);
                if (!await _authService.UpdateSessionUser(userModel,jti))
                {
                    return Unauthorized();
                }
                JwtTokenModel jwtTokenModel = new JwtTokenModel
                {
                    RefreshToken = newRefreshToken.Token,
                    RefreshTokenExpiration = newRefreshToken.Expiration
                };
                SetRefreshTokenCookie(jwtTokenModel);
                return Ok(new
                {
                    AccessToken = newAccessToken,
                });
            }
            return Unauthorized();
        }
        // TODO Убрать
        [HttpGet("getRefreshToken")]
        [SwaggerOperation(
            Summary = "Получить refresh токен",
            Description = "Этот метод получает активный refresh токен")]
        public string GetRefreshToken()
        {
            string value ="test";
            if (Request.Cookies["refreshToken"] != null)
            {
                 value = Request.Cookies["refreshToken"];
            }
            return value;
        }
        private void SetRefreshTokenCookie(JwtTokenModel token)
        {
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = token.RefreshTokenExpiration
                };
                if (token.RefreshToken != null)
                    Response.Cookies.Append("refreshToken", token.RefreshToken, cookieOptions);
            }
            catch
            {
                Console.WriteLine("Invalid token!");
            }
        }
    }
}