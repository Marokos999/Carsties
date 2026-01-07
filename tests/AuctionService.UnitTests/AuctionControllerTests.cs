using System;
using AuctionService.Data;
using AutoFixture;
using MassTransit;
using Moq;
using AutoMapper;
using AuctionService.Controllers;
using AuctionService.RequestHelpers;
using AuctionService.Entities;
using AuctionService.DTOs;

namespace AuctionService.UnitTests;

public class AuctionControllerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepo;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Fixture _fixture;
    private readonly AuctionsController _controller;
    private readonly IMapper _mapper;
        public AuctionControllerTests()
        {
            _fixture = new Fixture();
            _auctionRepo = new Mock<IAuctionRepository>();
            _publishEndpoint = new Mock<IPublishEndpoint>();
            var mockMapper = new MapperConfiguration(mc =>
            {
                mc.AddMaps(typeof(MappingProfiles).Assembly);
            }).CreateMapper().ConfigurationProvider;

            _mapper = new Mapper(mockMapper);
            _controller = new AuctionsController(_auctionRepo.Object, _mapper, _publishEndpoint.Object);
        }

    [Fact]
    public async Task GetAuctions_WithNoParams_Returns10Auctions()
    {
        // arrange

        var auction = _fixture.CreateMany<AuctionDto>(10).ToList();
        _auctionRepo.Setup(repo => repo.GetAuctionsAsync(null)).ReturnsAsync(auction);

        // act
        var result = await _controller.GetAuctions(null);

        // assert
        Assert.Equal(10, result.Value.Count);
        Assert.IsType<List<AuctionDto>>(result.Value);
    }
}
