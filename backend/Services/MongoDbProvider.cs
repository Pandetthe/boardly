using Boardly.Backend.Entities;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class MongoDbProvider
{
    public readonly IMongoDatabase Database;

    public readonly MongoClient Client;

    public MongoDbProvider(IConfiguration configuration)
    {
        string connectionString = configuration.GetValue<string>("MongoDb:ConnectionString")
            ?? throw new NullReferenceException("MongoDb's connection string configuration is missing.");
        string databaseName = configuration.GetValue<string>("MongoDb:DatabaseName")
            ?? throw new NullReferenceException("MongoDb's database name configuration is missing.");

        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCaseConvention", camelCaseConvention, t => true);
        Client = new(connectionString);
        Database = Client.GetDatabase(databaseName);
    }

    public IMongoCollection<User> GetUsersCollection() => Database.GetCollection<User>("users");

    public IMongoCollection<RefreshToken> GetRefreshTokensCollection() => Database.GetCollection<RefreshToken>("refreshTokens");
}
