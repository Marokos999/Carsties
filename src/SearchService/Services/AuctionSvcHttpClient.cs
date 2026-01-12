using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config)
{
    public async Task<List<Item>> GetItemsForSearchDb()
    {
        var db = await DB.InitAsync("SearchDb");

        var lastUpdated = await db.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString("O"))
            .ExecuteFirstAsync();

        var baseUrl = config["AuctionServiceUrl"] + "/api/auctions";
        var url = string.IsNullOrEmpty(lastUpdated) 
            ? baseUrl 
            : baseUrl + "?date=" + Uri.EscapeDataString(lastUpdated);

        var items = await httpClient.GetFromJsonAsync<List<Item>>(url);

        return items ?? [];
    }
}