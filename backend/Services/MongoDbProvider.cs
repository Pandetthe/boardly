using Boardly.Backend.Entities;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class MongoDbProvider
{
    private readonly IMongoDatabase _database;

    public MongoDbProvider(IConfiguration configuration)
    {
        string connectionString = configuration.GetValue<string>("MongoDb:ConnectionString")
            ?? throw new NullReferenceException("MongoDb's connection string configuration is missing.");
        string databaseName = configuration.GetValue<string>("MongoDb:DatabaseName")
            ?? throw new NullReferenceException("MongoDb's database name configuration is missing.");

        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCaseConvention", camelCaseConvention, t => true);
        MongoClient client = new(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<User> GetUsersCollection() => _database.GetCollection<User>("users");
}
