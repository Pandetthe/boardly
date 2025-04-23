using Boardly.Backend.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class MongoDbProvider
{
    public readonly IMongoDatabase Database;

    public readonly MongoClient Client;

    public MongoDbProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        string connectionString = configuration.GetValue<string>("MongoDb:ConnectionString")
            ?? throw new NullReferenceException("MongoDb's connection string configuration is missing.");
        string databaseName = configuration.GetValue<string>("MongoDb:DatabaseName")
            ?? throw new NullReferenceException("MongoDb's database name configuration is missing.");

        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCaseConvention", camelCaseConvention, t => true);
        var enumStringConvention = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };
        ConventionRegistry.Register("EnumStringConvention", enumStringConvention, t => true);

        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.LoggingSettings = new(loggerFactory);

        Client = new(settings);
        Database = Client.GetDatabase(databaseName);
    }

    public IMongoCollection<User> GetUsersCollection() => Database.GetCollection<User>("users");

    public IMongoCollection<RefreshToken> GetRefreshTokensCollection() => Database.GetCollection<RefreshToken>("refreshTokens");

    public IMongoCollection<Board> GetBoardsCollection() => Database.GetCollection<Board>("boards");
}
