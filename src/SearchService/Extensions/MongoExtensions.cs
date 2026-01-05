using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Entities;

namespace SearchService.Extensions;

public static class MongoExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("MongoDbConnection");
        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("MongoDB connection string not found in configuration.");

        var settings = MongoClientSettings.FromConnectionString(conn);

        var db = DB.InitAsync("SearchDb", settings).GetAwaiter().GetResult();

        services.AddSingleton(db);
        return services;
    }
}
