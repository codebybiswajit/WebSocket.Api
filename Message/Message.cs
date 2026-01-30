using MongoDB.Driver;

namespace Message
{
    public class MessageDb
    {
        private readonly IMongoCollection<ApplicationMessage> _message;
        public IMongoCollection<ApplicationMessage> GetCollection() { return _message; }

        public MessageDb(string connectionString, string databaseName = "WebsocDb", string collectionName = "WSUsers")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _message = db.GetCollection<ApplicationMessage>(collectionName);

            // Ensure unique indexes for email and username
            var creationdate = new CreateIndexModel<ApplicationMessage>(
                Builders<ApplicationMessage>.IndexKeys.Ascending(u => u.CreatedTime),
                new CreateIndexOptions { Unique = true, Name = "createdTime" }
            );
            _message.Indexes.CreateMany(new[] { creationdate });
        }

        public async Task<bool> AddAsync(ApplicationMessage _messageData)
        {
            bool res;
            try
            {
                var message = new ApplicationMessage
                {
                    Message = _messageData.Message,
                    Attachment = _messageData.Attachment,
                    CreatedTime = _messageData.CreatedTime,
                    CreatedBy = _messageData.CreatedBy,
                    //UpdatedBy = _messageData.UpdatedBy,
                };
                await _message.InsertOneAsync(message);
                res = true;
            }
            catch (MongoWriteException mex) when (mex.WriteError != null &&
                                                  (mex.WriteError.Category == ServerErrorCategory.DuplicateKey ||
                                                   mex.WriteError.Code == 11000))
            {
                res = false ;
            }
            catch (Exception ex)
            {
                res = false ;
            }
            return res;
        }
        public async Task<ApplicationMessage?> GetByIdAsync(string messageId)
        {
            var filter = Builders<ApplicationMessage>.Filter.Eq(u => u.Id, messageId);
            try
            {
                return await _message.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<List<ApplicationMessage>> GetAllMessagesAsync()
        {
            try
            {
                return await _message.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                return new List<ApplicationMessage>();
            }
        }
        public async Task<bool> DeleteByIdAsync(string messageId)
        {
            var filter = Builders<ApplicationMessage>.Filter.Eq(u => u.Id, messageId);
            try
            {
                var result = await _message.DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
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
            catch (Exception ex)
            {
                return false;
            }
        }

        


    }
}
