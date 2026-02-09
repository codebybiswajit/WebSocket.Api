using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace User
{
    [BsonIgnoreExtraElements]
    public class ApplicationUser
    {
        public ApplicationUser() {
            Groups = new List<Group>();
            Pairs = new List<Pair>();
            RefreshToken = new RefreshToken();
        }
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
        [BsonElement("groups"),BsonIgnoreIfNull]
        public List<Group> Groups { get; set; } = new List<Group>();
        [BsonElement("pairs"), BsonIgnoreIfNull]
        public List<Pair> Pairs { get; set; } = new List<Pair>();
        
        [BsonElement("createdDate")]
        public DateOnly CreatedDate { get; set; } = new DateOnly();

        [BsonElement("updatedDate")]
        public DateTime UpdatedDate { get; set; } = new DateTime();

        [BsonElement("refreshToken")]
        public RefreshToken RefreshToken { get; set; } = new RefreshToken();
    }
    public class RefreshToken
    {
        public string UserId { get; set; } = default!;
        public string TokenHash { get; set; } = default!;
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

        Active = 1,
        Deleted = 2,
        DeactivatedUser = 3
    }
    public class Pair {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }= default!;
       
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("createdDate")]
        public DateTime CreatedDate => DateTime.UtcNow;
    }
    public class Group {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        [BsonElement("contact")]
        public List<Pair> Members { get; set; } = new List<Pair>();

        [BsonElement("createdDate")]
        public DateTime CreatedDate => DateTime.UtcNow;
    }
}
