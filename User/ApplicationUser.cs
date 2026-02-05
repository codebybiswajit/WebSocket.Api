using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace User
{
    [BsonIgnoreExtraElements]
    public class ApplicationUser
    {
        /// <summary>
        /// Gets or sets the unique identifier for the document in the database.
        /// </summary>
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        /// <summary>
        /// username
        /// </summary>
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;
        [BsonElement("role")]
        public UserRole? Role { get; set; } = UserRole.Undefiened;
        [BsonElement("username")]
        public string Username  => $"{Email.ToLower().Split("@")[0]}-{Role?.ToString().ToLower()}";

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;
        [BsonElement("groups")]
        public List<Group> Groups { get; set; } = new List<Group>();
        [BsonElement("pairs")]
        public List<Group> Pairs { get; set; } = new List<Group>();
        
        [BsonElement("createdDate")]
        public DateOnly CreatedDate { get; set; } = new DateOnly();

        [BsonElement("updatedDate")]
        public DateTime UpdatedDate { get; set; } = new DateTime();

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

    public enum UserRole {
        [Display(Name = "Undefiened", Description ="Undefiened")]
        Undefiened  = 0,
        [Display(Name = "Admin", Description ="admin")]
        Admin = 1,
        [Display(Name = "User", Description = "user")]
        User = 2,
        [Display(Name = "Guest", Description = "guest")]
        Guest = 3
    }

    public enum ResStatus
    {

        Success = 1,
        Failure = 2,
        Duplicate = 3
    }
    public class Group {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
       
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
    }
}
