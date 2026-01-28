using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace User
{
    [BsonIgnoreExtraElements]
    public class ApplicationUser
    {
        /// <summary>
        /// Gets or sets the unique identifier for the document in the database.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// username
        /// </summary>
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("createdBy")]
        public CreatedBy CreatedBy{ get; set; } = new CreatedBy();

        [BsonElement("createdBy")]
        public CreatedBy Updatedby{ get; set; } = new CreatedBy();
        [BsonElement("refreshToken")]
        public RefreshToken RefreshToken { get; set; } = new RefreshToken();
    }
    public class RefreshToken
    {
        public string UserId { get; set; }
        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool Revoked { get; set; } = false;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !Revoked && !IsExpired;

    }
}
