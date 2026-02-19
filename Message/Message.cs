using MongoDB.Bson;
using MongoDB.Driver;

namespace Message
{
    public class MessageDb
    {
        private readonly IMongoCollection<ApplicationMessage> _message;

        public IMongoCollection<ApplicationMessage> GetCollection() { return _message; }

        public MessageDb(string connectionString, string databaseName, string collectionName = "WSMessage")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);

            // ✅ Assign FIRST, then create indexes
            _message = db.GetCollection<ApplicationMessage>(collectionName);

            var indexKeys = Builders<ApplicationMessage>.IndexKeys
                .Ascending(m => m.MessageFrom)
                .Ascending(m => m.MessageTo);
            _message.Indexes.CreateOne(new CreateIndexModel<ApplicationMessage>(indexKeys));
        }

        // ── Existing methods (unchanged) ──────────────────────────────────────

        public async Task<string> AddAsync(ApplicationMessage _messageData)
        {
            try
            {
                await _message.InsertOneAsync(_messageData);
                return "Message Retrieved successfully";
            }
            catch (MongoException mex) { return mex.Message; }
            catch (Exception ex) { return ex.Message; }
        }

        public async Task<ApplicationMessage> GetByIdAsync(string messageId)
        {
            var filter = Builders<ApplicationMessage>.Filter.Eq(u => u.Id, messageId);
            try { return await _message.Find(filter).FirstOrDefaultAsync(); }
            catch { return new ApplicationMessage(); }
        }

        public async Task<List<ApplicationMessage>> GetAllMessagesAsync()
        {
            try { return await _message.Find(_ => true).ToListAsync(); }
            catch { return new List<ApplicationMessage>(); }
        }

        public async Task<bool> DeleteByIdAsync(string messageId)
        {
            var filter = Builders<ApplicationMessage>.Filter.Eq(u => u.Id, messageId);
            try
            {
                var result = await _message.DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateMessageAsync(string messageId, ApplicationMessage updatedMessage)
        {
            var filter = Builders<ApplicationMessage>.Filter.Eq(u => u.Id, messageId);
            var update = Builders<ApplicationMessage>.Update
                .Set(u => u.Message, updatedMessage.Message)
                .Set(u => u.Attachment, updatedMessage.Attachment)
                .Set(u => u.UpdatedBy, updatedMessage.UpdatedBy);
            try
            {
                var result = await _message.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch { return false; }
        }

        // ── New: conversation query methods ───────────────────────────────────

        /// <summary>
        /// Save a new message — wraps AddAsync with a fully constructed ApplicationMessage.
        /// </summary>
        public async Task<ApplicationMessage?> SaveMessageAsync(
            string fromUserId,
            string fromUsername,
            string toId,          // userId for Pair, groupId for Group
            string content,
            ChatType chatType = ChatType.Pair)
        {
            var message = new ApplicationMessage
            {
                Id = ObjectId.GenerateNewId().ToString(),
                MessageFrom = fromUserId,
                MessageTo = chatType == ChatType.Pair ? toId : null,
                MessageIn = chatType == ChatType.Group ? toId : null,
                Message = content,
                ChatType = chatType,
                CreatedBy = new CreatedBy { Id = fromUserId, Name = fromUsername },
                UpdatedBy = new CreatedBy { Id = fromUserId, Name = fromUsername },
            };

            var result = await AddAsync(message);

            // AddAsync returns a success string — if it doesn't contain "success" something went wrong
            return result.Contains("success", StringComparison.OrdinalIgnoreCase)
                ? message
                : null;
        }

        /// <summary>
        /// Get private conversation between two users, ordered by date, paginated.
        /// </summary>
        public async Task<List<ApplicationMessage>> GetConversationAsync(
            string userId1,
            string userId2,
            int page = 0,
            int pageSize = 50)
        {
            var filter = Builders<ApplicationMessage>.Filter.And(
                Builders<ApplicationMessage>.Filter.Eq(m => m.ChatType, ChatType.Pair),
                Builders<ApplicationMessage>.Filter.Or(
                    Builders<ApplicationMessage>.Filter.And(
                        Builders<ApplicationMessage>.Filter.Eq(m => m.MessageFrom, userId1),
                        Builders<ApplicationMessage>.Filter.Eq(m => m.MessageTo, userId2)
                    ),
                    Builders<ApplicationMessage>.Filter.And(
                        Builders<ApplicationMessage>.Filter.Eq(m => m.MessageFrom, userId2),
                        Builders<ApplicationMessage>.Filter.Eq(m => m.MessageTo, userId1)
                    )
                )
            );

            try
            {
                return await _message
                    .Find(filter)
                    .SortBy(m => m.CreatedBy.Date)
                    .Skip(page * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch { return new List<ApplicationMessage>(); }
        }

        /// <summary>
        /// Get all messages in a group, ordered by date, paginated.
        /// </summary>
        public async Task<List<ApplicationMessage>> GetGroupMessagesAsync(
            string groupId,
            int page = 0,
            int pageSize = 50)
        {
            var filter = Builders<ApplicationMessage>.Filter.And(
                Builders<ApplicationMessage>.Filter.Eq(m => m.ChatType, ChatType.Group),
                Builders<ApplicationMessage>.Filter.Eq(m => m.MessageIn, groupId)
            );

            try
            {
                return await _message
                    .Find(filter)
                    .SortBy(m => m.CreatedBy.Date)
                    .Skip(page * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch { return new List<ApplicationMessage>(); }
        }

        /// <summary>
        /// Get the most recent message per conversation for a user (contact list previews).
        /// </summary>
        public async Task<Dictionary<string, ApplicationMessage>> GetLastMessagesAsync(string userId)
        {
            var filter = Builders<ApplicationMessage>.Filter.And(
                Builders<ApplicationMessage>.Filter.Eq(m => m.ChatType, ChatType.Pair),
                Builders<ApplicationMessage>.Filter.Or(
                    Builders<ApplicationMessage>.Filter.Eq(m => m.MessageFrom, userId),
                    Builders<ApplicationMessage>.Filter.Eq(m => m.MessageTo, userId)
                )
            );

            try
            {
                var messages = await _message
                    .Find(filter)
                    .SortByDescending(m => m.CreatedBy.Date)
                    .ToListAsync();

                // Latest message per conversation partner
                return messages
                    .GroupBy(m => m.MessageFrom == userId ? m.MessageTo : m.MessageFrom)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            catch { return new Dictionary<string, ApplicationMessage>(); }
        }

        /// <summary>
        /// Count unread messages sent TO a user, grouped by sender.
        /// Note: add an IsRead bool to ApplicationMessage to make this precise.
        /// For now returns total received per sender.
        /// </summary>
        public async Task<Dictionary<string, int>> GetUnreadCountsAsync(string userId)
        {
            var filter = Builders<ApplicationMessage>.Filter.And(
                Builders<ApplicationMessage>.Filter.Eq(m => m.ChatType, ChatType.Pair),
                Builders<ApplicationMessage>.Filter.Eq(m => m.MessageTo, userId)
            );

            try
            {
                var messages = await _message.Find(filter).ToListAsync();
                return messages
                    .GroupBy(m => m.MessageFrom)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
            catch { return new Dictionary<string, int>(); }
        }
    }
}