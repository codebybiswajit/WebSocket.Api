using Message;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace User
{
    public class UserDb
    {
        private readonly IMongoCollection<ApplicationUser> _users;
        public IMongoCollection<ApplicationUser> GetCollection() { return _users; }

        public UserDb(string connectionString, string databaseName = "WebsocDb", string collectionName = "WSUsers")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _users = db.GetCollection<ApplicationUser>(collectionName);

            // Ensure unique indexes for email and username
            var emailIndex = new CreateIndexModel<ApplicationUser>(
                Builders<ApplicationUser>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_email" }
            );

            var usernameIndex = new CreateIndexModel<ApplicationUser>(
                Builders<ApplicationUser>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "ux_username" }
            );

            _users.Indexes.CreateMany(new[] { emailIndex, usernameIndex });
        }

        public async Task<object> AddAsync(ApplicationUser _user)
        {
            var user = new ApplicationUser
            {
                Name = _user.Name,
                Email = _user.Email,
                Role = _user.Role,
                Password = PasswordHelper.HashPassword(_user.Password),
            };

            try
            {
                await _users.InsertOneAsync(user);
                return new {message = "Created SuccessFully", id = user.Id, Name = user.Name};
            }
            catch (MongoWriteException mex) when (mex.WriteError != null &&
                                                  (mex.WriteError.Category == ServerErrorCategory.DuplicateKey ||
                                                   mex.WriteError.Code == 11000))
            {
                return new { message = "Email already exist try again with different one" };
            }
            catch (Exception ex)
            {
                return new { message = ex.Message };
            }
        }

        public async Task<ApplicationUser> GetByIdAsync(string userId)
        {
            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Id,  userId);
            try
            {
                return await _users.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception err)
            {
                return new ApplicationUser();
            }
        }

        public async Task<List<ApplicationUser>> GetAllAsync()
        {
            var respose = new List<ApplicationUser>();
            try
            {
                var rec = await _users.Find(Builders<ApplicationUser>.Filter.Empty).ToListAsync();
                return rec;
            }
            catch (Exception error)
            {
               
            }
            return respose;
        }

        public async Task<bool> UpdateAsync(ApplicationUser _user, string userId)
        {
            var updates = new List<UpdateDefinition<ApplicationUser>>();
            if (!string.IsNullOrWhiteSpace(_user.Username))
                updates.Add(Builders<ApplicationUser>.Update.Set(u => u.Username, _user.Username.Trim()));
            if (!string.IsNullOrWhiteSpace(_user.Email))
                updates.Add(Builders<ApplicationUser>.Update.Set(u => u.Email, _user.Email.Trim().ToLowerInvariant()));
            
            updates.Add(Builders<ApplicationUser>.Update.Set(u => u.UpdatedDate, new DateTime().Date));

            var update = Builders<ApplicationUser>.Update.Combine(updates);
            var result = await _users.UpdateOneAsync(Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId), update);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }
        public async Task<bool> CreateGroup(Group newGroup, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required.", nameof(userId));
            if (newGroup == null) throw new ArgumentNullException(nameof(newGroup));

            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId);

            var update = Builders<ApplicationUser>.Update.AddToSet(u => u.Pairs, newGroup );

            var result = await _users.UpdateOneAsync(filter, update);

            return result.IsAcknowledged && result.MatchedCount == 1;
        }
        public async Task<bool> CreatePair(Group newGroup, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required.", nameof(userId));
            if (newGroup == null) throw new ArgumentNullException(nameof(newGroup));

            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId);

            var update = Builders<ApplicationUser>.Update.AddToSet(u => u.Groups, newGroup);

            var result = await _users.UpdateOneAsync(filter, update);

            return result.IsAcknowledged && result.MatchedCount == 1;
        }
        public async Task<bool> AddToGroup(Group newGroup, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required.", nameof(userId));
            if (newGroup == null) throw new ArgumentNullException(nameof(newGroup));

            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId);

            // $push always appends to the end (next index)
            var update = Builders<ApplicationUser>.Update.Push(u => u.Groups, newGroup);

            var result = await _users.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.MatchedCount == 1 && result.ModifiedCount == 1;
        }
        public async Task<bool> RemoveFromGroupByObject(string userId, Group newGroup)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required.", nameof(userId));
            if (newGroup == null) throw new ArgumentNullException(nameof(newGroup));

            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId);
            var update = Builders<ApplicationUser>.Update.Pull(u => u.Groups, newGroup);

            var result = await _users.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.MatchedCount == 1 && result.ModifiedCount == 1;
        }

        public async Task<bool> DeleteAsync(string userId)
        {
            var result = await _users.DeleteOneAsync(Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId));
            return result.IsAcknowledged && result.DeletedCount == 1;
        }

        public class PasswordHelper
        {
            public static string HashPassword(string plainPassword)
            {
                return BCrypt.Net.BCrypt.HashPassword(plainPassword);
            }

            public static bool VerifyPassword(string plainPassword, string hashedPassword)
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            }
        }

        public async Task<bool> AddRefreshTokenAsync(string userId, RefreshToken refreshtoken)
        {
            var update = Builders<ApplicationUser>.Update.Set(u => u.RefreshToken, refreshtoken);

            var result = await _users.UpdateOneAsync(
                Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId),
                update
            );

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

    }
}
