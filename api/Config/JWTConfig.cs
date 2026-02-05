namespace api.Config
{
    public class JWTConfig
    {
        public string Key { get; set; }
    }
    public class AccessTokens
    {
        public string AccessToken { get; set; }
        public int ExpiryTime { get; set; }
    }
    public class RefreshTokens
    {
        public string RefreshToken { get; set; }
        public int ExpiryDays { get; set; }
    }
}
