using System;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;

namespace SearchService.Data;

public class DbInitializer
{
    public static async Task InitDb(WebApplication app)
    {
        var conn =  app.Configuration.GetConnectionString("MongoDbConnection");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("MongoDB connection string not found in configuration.");

        var db = await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(conn));

        await db.Index<Item>()
            .Key(x => x.Make, KeyType.Text)
            .Key(x => x.Model, KeyType.Text)
                .Key(x => x.Color, KeyType.Text)
                .CreateAsync();


        var count = await db.CountAsync<Item>();
        using var scope = app.Services.CreateScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();

        var items = await httpClient.GetItemsForSearchDb();

        Console.WriteLine($"Items count: {items.Count}");

        if (items.Count > 0) await db.SaveAsync(items);
    }
}