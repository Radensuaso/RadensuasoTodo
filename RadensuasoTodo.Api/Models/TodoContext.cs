using MongoDB.Driver;

namespace RadensuasoTodo.Api.Models
{
    public class MongoDbContext(IMongoClient mongoClient, string databaseName)
    {
        private readonly IMongoDatabase _database = mongoClient.GetDatabase(databaseName);

        public IMongoCollection<TodoItem> TodoItems => _database.GetCollection<TodoItem>("TodoItems");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}
