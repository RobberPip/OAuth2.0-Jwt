namespace DbProvider.Models;

public class JwtTokenModel
{
    public DateTime RefreshTokenExpiration { get; set; }
    public Guid? RefreshTokenJti { get; set; }
}