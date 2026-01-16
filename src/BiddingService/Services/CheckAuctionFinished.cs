using System;
using BiddingService.Models;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Services;

public class CheckAuctionFinished(ILogger<CheckAuctionFinished> logger, IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting check for finished auctions");


        stoppingToken.Register(() => logger.LogInformation("==> Auction check stopping"));

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAuctions(stoppingToken);

            await Task.Delay(5000, stoppingToken);
    }
}

    private async Task CheckAuctions(CancellationToken stoppingToken)
    {
        var db = await DB.InitAsync("BidDb");
        
        var finishedAuctions = await db.Find<Models.Auction>()
            .Match(a => a.AuctionEnd < DateTime.UtcNow)
            .Match(a => a.Finished == false)
            .ExecuteAsync(stoppingToken);

        if (finishedAuctions.Count == 0) return;
        
            logger.LogInformation($"==> Found {finishedAuctions.Count} auctions that have completed");

         using var scope = services.CreateScope();
        var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();


        foreach (var auction in finishedAuctions)
        {
            auction.Finished = true;
            await db.SaveAsync(auction, stoppingToken);
            

            var winningBid = await db.Find<Models.Bid>()
                .Match(b => b.AuctionId == auction.ID)
                .Match(b => b.BidStatus == Models.BidStatus.Accepted || b.BidStatus == Models.BidStatus.AcceptedBelowReserve)
                .Sort(b => b.Descending(x => x.Amount))
                .ExecuteFirstAsync(stoppingToken);

            await endpoint.Publish(new AuctionFinished
            {
                ItemSold = winningBid is not null,
                AuctionId = auction.ID,
                Winner = winningBid?.Bidder,
                Amount = winningBid?.Amount,
                Seller = auction.Seller
            }, stoppingToken);
        }
        
    }
}