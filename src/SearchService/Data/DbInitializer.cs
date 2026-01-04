using System;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;

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
        if (count == 0)
        {
            Console.WriteLine("No data - will attempt to seed");
            var itemData = await File.ReadAllTextAsync("Data/auctions.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<Item>>(itemData, options);

            if (items is not null)
            {
                await db.SaveAsync(items);
            }
        }
    }
}