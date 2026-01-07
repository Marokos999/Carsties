using System;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("--> Consuming bid placed");

        var db = await DB.InitAsync("SearchDb");

        var auction = await db.Find<Item>().OneAsync(context.Message.AuctionId)
            ?? throw new MessageException(typeof(AuctionFinished), "Cannot retrieve this auction");
        
        if (context.Message.ItemSold)
        {
            auction.Winner = context.Message?.Winner;
            auction.SoldAmount = context.Message?.Amount ?? 0;
        }

        auction.Status = "Finished";

        await db.SaveAsync(auction);
    }
}