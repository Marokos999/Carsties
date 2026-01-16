using System;
using BiddingService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace BiddingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Bid>> PlaceBid(string auctionId, int amount)
    {
        var db = await DB.InitAsync("BidDB");

        var auction = await db.Find<Auction>().OneAsync(auctionId);
        if(auction is null)
        {
            return NotFound("Auction not found");
        }

        if(auction.Seller == User.Identity?.Name)
        {
            return BadRequest("Sellers cannot bid on their own auctions");
        }


        var bid = new Bid
        {
            AuctionId = auctionId,
            Bidder = User.Identity?.Name!,
            Amount = amount,
        };

        if (auction.AuctionEnd < DateTime.UtcNow)
        {
            bid.BidStatus = BidStatus.Finished;
        }
        else
        {
            var highBid = await db.Find<Bid>()
                    .Match(a => a.AuctionId == auctionId)
                    .Sort(b => b.Descending(x => x.Amount))
                    .ExecuteFirstAsync();

            if (highBid is not null && amount > highBid.Amount || highBid is null)
                {
                    bid.BidStatus = amount > auction.ReservePrice
                        ? BidStatus.Accepted
                        : BidStatus.AcceptedBelowReserve;
                }

                if (highBid is not null && bid.Amount <= highBid.Amount)
                {
                    bid.BidStatus = BidStatus.TooLow;
                }
        }
        await db.SaveAsync(bid);

        return Ok(bid);
    }        

        [HttpGet("{auctionId}")]
    public async Task<ActionResult<List<Bid>>> GetBidsForAuction(string auctionId)
    {
        var db = await DB.InitAsync("BidDB");
        
        var bids = await db.Find<Bid>()
            .Match(a => a.AuctionId == auctionId)
            .Sort(b => b.Descending(a => a.BidTime))
            .ExecuteAsync();

        return bids;
    }
}
