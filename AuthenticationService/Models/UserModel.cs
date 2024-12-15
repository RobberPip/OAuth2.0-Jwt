namespace AuthenticationService.Models
{
    public class UserModel
    {
        public Guid Uid { get; set; }
        public string? UserName { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? SaltPassword { get; set; }
        public JwtTokenModel JwtTokens = new JwtTokenModel();
    }
}
