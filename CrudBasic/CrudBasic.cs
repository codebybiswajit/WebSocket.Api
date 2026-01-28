using MongoDB.Driver;

namespace CrudBasic
{
    public interface ICrudBasic<T>
    {
        Task<T> AddAsync(T entity);
        Task<T?> GetByIdAsync(string id);
        Task<List<T>> GetAllAsync();
        Task<bool> DeleteAsync(string id);
        IMongoCollection<T> Collection { get; }
    }

    public class CrudBasic<T> : ICrudBasic<T>
    {
        public IMongoCollection<T> Collection { get; }

        public CrudBasic(MongoDbContext context, string collectionName)
        {
            Collection = context.GetCollection<T>(collectionName);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await Collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task<T?> GetByIdAsync(string id)
        {
            // assumes your documents have "Id" field (string)
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync();
        }

        public virtual async Task<bool> DeleteAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            var result = await Collection.DeleteOneAsync(filter);
            return result.IsAcknowledged && result.DeletedCount == 1;
        }
    }
}
``