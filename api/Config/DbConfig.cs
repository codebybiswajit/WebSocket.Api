using User;

namespace api.Config
{
    public class DbManager
    {
        public UserDb UserDb { get; }
        

        public DbManager(string connectionString)
        {
            UserDb = new UserDb(connectionString);
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
