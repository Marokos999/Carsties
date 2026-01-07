using System;
using AuctionService.DTOs;
using AuctionService.Entities;

namespace AuctionService.Data;

public interface IAuctionRepository
{
    Task<List<AuctionDto>> GetAuctionsAsync(string date);
    Task<AuctionDto> GetAuctionByIdAsync(Guid id);
    Task<Auction> GetAuctionEntityByIdAsync(Guid id);

    void AddAuctionAsync(Auction auction);
    void RemoveAuction(Auction auction);
    Task<bool> SaveChangesAsync();
}
