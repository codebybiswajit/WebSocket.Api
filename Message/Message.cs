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
            _message = db.GetCollection<ApplicationMessage>(collectionName);
        }

        public async Task<string> AddAsync(ApplicationMessage _messageData)
        {
            string res;
            try
            {
                await _message.InsertOneAsync(_messageData);
                res = "Message Retrived succfully";
            }
            catch (MongoException mex)
            {
                res = mex.Message ;
            }
            catch (Exception ex)
            {
                res = ex.Message ;
            }
            return res;
        }
        public async Task<ApplicationMessage> GetByIdAsync(string userId)
        {
            var filter = Builders<ApplicationMessage>.Filter.Eq(u => u.Id, userId);
            try
            {
                return await _message.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception err)
            {
                return new ApplicationMessage();
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
