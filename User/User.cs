using MongoDB.Driver;

namespace User
{
    public class UserDb
    {
        private readonly IMongoCollection<ApplicationUser> _users;

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

        public async Task<bool> AddAsync(ApplicationUser _user)
        {
            var user = new ApplicationUser
            {
                Name = _user.Name,
                Username = _user.Username,
                Email = _user.Email,
                Role = _user.Role,
                Password = PasswordHelper.HashPassword(_user.Password),
            };

            try
            {
                await _users.InsertOneAsync(user);
                return true;
            }
            catch (MongoWriteException mex) when (mex.WriteError != null &&
                                                  (mex.WriteError.Category == ServerErrorCategory.DuplicateKey ||
                                                   mex.WriteError.Code == 11000))
            {
                return false ;
            }
            catch (Exception ex)
            {
                return false ;
            }
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId);
            try
            {
                return await _users.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception err)
            {
                return null;
            }
        }

        //public async Task<List<ApplicationUserDBResponse>> GetAllAsync()
        //{
        //    var respose = new List<ApplicationUserDBResponse>();
        //    try
        //    {
        //        var rec = await _users.Find(Builders<ApplicationUser>.Filter.Empty).ToListAsync();
        //        foreach (var item in rec)
        //        {
        //            respose.Add(new ApplicationUserDBResponse()
        //            {
        //                Id = item.Id,
        //                Name = item.Name,
        //                Username = item.Username,
        //                Role = item.Role,
        //                //Password = item.Password, //not required to show to user if requred enable latter
        //                CreatedAt = item.CreatedAt,
        //                UpdatedAt = item.UpdatedAt,
        //                ContactDetails = item?.ContactDetails ?? new UserContactDetails(),
        //                Error = null
        //            });
        //        }
        //    }
        //    catch (Exception error)
        //    {
        //        ErrorResponseDb err = new ErrorResponseDb()
        //        {
        //            Message = "Error fetching users",
        //            Error = new Errors()
        //            {
        //                Message = error.Message,
        //                Status = "500"
        //            }
        //        };
        //    }
        //    return respose;
        //}

        public async Task<bool> UpdateAsync(ApplicationUser _user, string userId)
        {
            var updates = new List<UpdateDefinition<ApplicationUser>>();
            if (!string.IsNullOrWhiteSpace(_user.Username))
                updates.Add(Builders<ApplicationUser>.Update.Set(u => u.Username, _user.Username.Trim()));
            if (!string.IsNullOrWhiteSpace(_user.Email))
                updates.Add(Builders<ApplicationUser>.Update.Set(u => u.Email, _user.Email.Trim().ToLowerInvariant()));
            Message.CreatedBy mcb = new Message.CreatedBy
            {
                Id = _user.Updatedby.Id,
                Name = _user.Updatedby.Name,
                Date = DateTime.UtcNow
            };

            updates.Add(Builders<ApplicationUser>.Update.Set(u => u.Updatedby, mcb));

            var update = Builders<ApplicationUser>.Update.Combine(updates);
            var result = await _users.UpdateOneAsync(Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId), update);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> DeleteAsync(string userId)
        {
            var result = await _users.DeleteOneAsync(Builders<ApplicationUser>.Filter.Eq(u => u.Id, userId));
            return result.IsAcknowledged && result.DeletedCount == 1;
        }

        public IMongoCollection<ApplicationUser> GetCollection()
        {
            return _users;
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
