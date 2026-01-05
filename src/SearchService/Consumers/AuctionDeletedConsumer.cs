using System;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
{
    public async Task Consume(ConsumeContext<AuctionDeleted> context)
    {
        Console.WriteLine($"Auction deleted: {context.Message.Id}");

        var db = await DB.InitAsync("SearchDb");        

        var result = await db.DeleteAsync<Item>(context.Message.Id);
        
        if (!result.IsAcknowledged)
            throw new MessageException(typeof(AuctionDeleted), "Failed to delete auction");
    }
}