#r "nuget: MongoDB.Entities, 24.2.0"

using MongoDB.Entities;
using System;

// Initialize MongoDB
await DB.InitAsync("BidDb", MongoClientSettings.FromConnectionString("mongodb://root:mongopw@localhost"));

// Create test bid
var bid = new
{
    AuctionId = "afbee524-5972-4075-8800-7d1f9d7b0a0c",
    Bidder = "bob",
    Amount = 10000,
    BidTime = DateTime.UtcNow,
    BidStatus = 0 // Accepted
};

Console.WriteLine("Test bid creation script - MongoDB manually");
Console.WriteLine($"Auction: {bid.AuctionId}, Amount: {bid.Amount}, Status: {bid.BidStatus}");
