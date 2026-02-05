
using api.Config;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using User;

namespace api.Middleware
{
    public class GetAuth
    {
        private readonly JWTConfig _jwt;
        private readonly AccessTokens _acc;
        private readonly RefreshTokens _ref;
        private readonly DbManager _db;

        public GetAuth(JWTConfig jwt, AccessTokens acc, RefreshTokens refresh, DbManager db)
        {
            _jwt = jwt;
            _acc = acc;
            _ref = refresh;
            _db = db;
        }

        public int GetRefreshExpiryDays()
        {
            var days = _ref?.ExpiryDays ?? 3;
            return days <= 0 ? 3 : days;
        }

        public int GetAccessExpiryMinutes()
        {
            var mins = _acc?.ExpiryTime ?? 30;
            return mins <= 0 ? 30 : mins;
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Generates a signed JSON Web Token (JWT) for the specified user.
        /// The token contains claims for "UserId", "Username" and the user's role, and is signed using the configured HMAC-SHA256 key.
        /// </summary>
        /// <param name="userId">The identifier of the user for whom to generate the token.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> that resolves to the serialized JWT string.
        /// </returns>
        /// <remarks>
        /// The method retrieves user details from `_db.UserDb.GetByIdAsync`. If the user record is not found,
        /// the "Username" claim will be an empty string and the role claim may be null or empty — callers should
        /// ensure the user exists if role information is required. Token expiration is set using `_acc.ExpiryTime` (minutes).
        /// </remarks>
        public async Task<string> GenerateJwtToken(string userId)
        {
            var res = await _db.UserDb.GetByIdAsync(userId);

            // Determine role name safely. Map numeric values to role names as needed.
           
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("UserId", userId),
                    new Claim("Username", res?.Name ?? string.Empty),
                    new Claim(ClaimTypes.Role, res?.Role.ToString() ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddMinutes(GetAccessExpiryMinutes()),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        /// <summary>
        /// Generates a secure refresh token (raw) and stores a hashed copy in the DB.
        /// Returns the raw token that should be returned to the client (store hashed server-side).
        /// </summary>
        public async Task<string> GenerateAndStoreRefreshTokenAsync(string userId, TimeSpan? lifetime = null)
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            var rawToken = Base64UrlEncode(bytes);

            var hash = ComputeSha256Hash(rawToken);
            var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(GetRefreshExpiryDays()));

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = hash,
                ExpiresAt = expires,
                CreatedAt = DateTime.UtcNow,
            };

            await _db.UserDb.AddRefreshTokenAsync(userId, refreshToken);
            return rawToken;
        }

    }
}




