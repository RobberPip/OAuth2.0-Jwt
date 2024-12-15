using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthenticationService.Models;
using System.Text;
using AuthenticationService.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace AuthenticationService.Services
{
    public class AuthService
    {
        public AuthService(IOptions<JwtSettingsModel> jwtSettings, HttpClient httpClient)
        {
            _jwtSettingsModel = jwtSettings.Value;
            _httpClient = httpClient;
        }

        private readonly JwtSettingsModel _jwtSettingsModel;
        private readonly HttpClient _httpClient;

        public async Task<UserModel?> AuthenticationUserAsync(string? userLogin, string? userPassword)
        {
            if (string.IsNullOrWhiteSpace(userLogin) || string.IsNullOrWhiteSpace(userPassword))
            {
                return null;
            }

            var response = await _httpClient.GetAsync($"{Links.DbProvider}/api/User/GetUser?login={userLogin}");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserModel>();
                if (user != null)
                {
                    // Хэширование пароля
                    string? hashedPassword = Cryptography.HashPassword(userPassword);
                    string? hashedPasswordSalt = Cryptography.ApplySalt(hashedPassword, user.SaltPassword);
                    if (user.Password == hashedPasswordSalt)
                    {
                        // Генерация JWT токенов
                        JwtTokenModel tokens = user.JwtTokens;
                        tokens.AccessToken = GenerateAccessToken(user.Uid.ToString());
                        (tokens.RefreshToken, tokens.RefreshTokenExpiration) =
                            GenerateRefreshToken(user.Uid.ToString());
                        tokens.RefreshTokenJti = GetJtiFromToken(tokens.RefreshToken);
                        if (!await AddSessionUser(user))
                        {
                            return null;
                        }
                        return user;
                    }
                }
            }

            return null;
        }

        public Guid? GetJtiFromToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var jtiClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Jti);
            if (jtiClaim != null && Guid.TryParse(jtiClaim.Value, out var jtiGuid))
            {
                return jtiGuid;
            }

            return null;
        }

        /// <summary>
        /// Метод для генерации Access Token
        /// </summary>
        /// <param name="userUid">Uid пользователя</param>
        /// <returns>AccessToken</returns>
        public string? GenerateAccessToken(string? userUid)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userUid),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(_jwtSettingsModel.Secret))
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettingsModel.Secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _jwtSettingsModel.Issuer,
                    audience: _jwtSettingsModel.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettingsModel.AccessTokenExpirationMinutes), // Используем UtcNow
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            throw new InvalidOperationException("JWT secret is not configured.");
        }

        /// <summary>
        /// Метод для генерации Refresh Token
        /// </summary>
        /// <param name="userUid">Uid пользователя</param>
        /// <returns>RefreshToken, время жизни ключа</returns>
        public (string? Token, DateTime Expiration) GenerateRefreshToken(string? userUid)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userUid),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (_jwtSettingsModel.Secret != null)
            {
                SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettingsModel.Secret));
                SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                DateTime expiration = DateTime.Now.AddDays(_jwtSettingsModel.RefreshTokenExpirationDays);
                JwtSecurityToken token = new JwtSecurityToken(
                    issuer: _jwtSettingsModel.Issuer,
                    audience: _jwtSettingsModel.Audience,
                    claims: claims,
                    expires: expiration,
                    signingCredentials: creds);
                return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
            }

            return (null, DateTime.MinValue);
            ;
        }

        public async Task<bool> ValidateRefreshToken(Guid userUid, Guid? refreshTokenJti)
        {
            if (Guid.Empty.Equals(userUid) || Guid.Empty.Equals(refreshTokenJti))
            {
                return false;
            }
            var uri = $"{Links.DbProvider}/api/User/GetSessionUser?refreshTokenJti={refreshTokenJti}";
            var response = await _httpClient.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonConvert.DeserializeObject(jsonResponse);
                if (tokenInfo != null)
                {
                    JwtTokenModel? jwtTokenModel = JsonConvert.DeserializeObject<JwtTokenModel>(
                        JObject.Parse(jsonResponse)["jwtTokens"]?.ToString() ?? string.Empty
                    );
                    if (jwtTokenModel != null && jwtTokenModel.RefreshTokenExpiration <= DateTime.UtcNow)
                    {
                        await DeleteSessionUser(userUid,jwtTokenModel.RefreshTokenJti);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> AddSessionUser(UserModel userModel)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(userModel, jsonSettings), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{Links.DbProvider}/api/User/AddSessionUser", jsonContent);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSessionUser(UserModel? userModel, Guid? oldRefreshTokenJti)
        {
            if (userModel == null)
                throw new ArgumentNullException(nameof(userModel));

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            var payload = new
            {
                User = userModel,
                OldRefreshTokenJti = oldRefreshTokenJti
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(payload, jsonSettings), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{Links.DbProvider}/api/User/UpdateSessionUser", jsonContent);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> DeleteSessionUser(Guid uidUser,Guid? refreshTokenJti)
        {
            var query = $"?userId={uidUser}&sessionId={refreshTokenJti}";
            var response = await _httpClient.DeleteAsync($"{Links.DbProvider}/api/User/DeleteSessionUser{query}");
            return response.IsSuccessStatusCode;
        }

        public UserModel? ExtractClaimsFromRefreshToken(string? refreshToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(refreshToken))
                {
                    JwtSecurityToken? jwtToken = handler.ReadJwtToken(refreshToken);
                    string? userUid = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    string? jti = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
                    DateTime expiration = jwtToken.ValidTo;
                    if (userUid != null && jti != null)
                    {
                        UserModel userModel = new UserModel()
                        {
                            Uid = Guid.Parse(userUid),
                            JwtTokens =
                            {
                                RefreshTokenJti = Guid.Parse(jti),
                                RefreshTokenExpiration = expiration
                            }
                        };
                        return userModel;
                    }
                }
                return null;
            }
            catch
            {
                Console.WriteLine("No valid token");
                return null;
            }
        }
    }
}