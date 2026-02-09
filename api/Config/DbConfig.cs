using Message;
using User;

namespace api.Config
{
    public class DbManager
    {
        public UserDb UserDb { get; }
        public MessageDb MessageDb { get; }

        public DbManager(string connectionString , string databaseName)
        {
            UserDb = new UserDb(connectionString,databaseName);
            MessageDb = new MessageDb(connectionString ,databaseName);
            //ProductDb = new ProductDB(connectionString);
            //ProjectDb = new ProjectDB(connectionString);
        }
    }

    public class DbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
