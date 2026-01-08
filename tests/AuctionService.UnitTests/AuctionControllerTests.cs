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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Contracts;
using System.Security.Claims;

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
            _controller = new AuctionsController(_auctionRepo.Object, _mapper, _publishEndpoint.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(
                            new ClaimsIdentity(
                                new[] { new Claim(ClaimTypes.Name, "testuser") },
                                authenticationType: "TestAuth")
                        )
                    }
                }
            };

            // Ensure awaited publish calls don't return null tasks
            _publishEndpoint
                .Setup(p => p.Publish(It.IsAny<AuctionCreated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _publishEndpoint
                .Setup(p => p.Publish(It.IsAny<AuctionUpdated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _publishEndpoint
                .Setup(p => p.Publish(It.IsAny<AuctionDeleted>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Also cover generic overloads
            _publishEndpoint
                .Setup(p => p.Publish<AuctionCreated>(It.IsAny<AuctionCreated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _publishEndpoint
                .Setup(p => p.Publish<AuctionUpdated>(It.IsAny<AuctionUpdated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _publishEndpoint
                .Setup(p => p.Publish<AuctionDeleted>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

    [Fact]
    public async Task CreateAuction_SaveFails_ReturnsBadRequest()
    {
        // arrange
        var createDto = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(r => r.AddAuctionAsync(It.IsAny<Auction>()));
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // act
        var result = await _controller.CreateAuction(createDto);

        // assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
        _auctionRepo.Verify(r => r.AddAuctionAsync(It.IsAny<Auction>()), Times.Once);
    }

    [Fact]
    public async Task CreateAuction_NoIdentity_Throws()
    {
        // arrange
        var createDto = _fixture.Create<CreateAuctionDto>();
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // act/assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _controller.CreateAuction(createDto));
        Assert.Equal("User identity not found", ex.Message);
    }

    [Fact]
    public async Task UpdateAuction_NotFound_ReturnsNotFound()
    {
        // arrange
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Auction)null);

        // act
        var result = await _controller.UpdateAuction(Guid.NewGuid(), new UpdateAuctionDto());

        // assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_SellerMismatch_ReturnsForbid()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "someoneelse",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/1.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auctionEntity);

        // act
        var result = await _controller.UpdateAuction(auctionEntity.Id, new UpdateAuctionDto { Make = "Honda" });

        // assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_SaveSucceeds_ReturnsOk_AndPublishes()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "testuser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/2.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.UpdateAuction(auctionEntity.Id, new UpdateAuctionDto { Make = "Honda" });

        // assert
        Assert.IsType<OkResult>(result.Result);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<AuctionUpdated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAuction_SaveFails_ReturnsBadRequest()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "testuser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/3.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // act
        var result = await _controller.UpdateAuction(auctionEntity.Id, new UpdateAuctionDto { Make = "Honda" });

        // assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAuction_NotFound_ReturnsNotFound()
    {
        // arrange
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Auction)null);

        // act
        var result = await _controller.DeleteAuction(Guid.NewGuid());

        // assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_SellerMismatch_ReturnsForbid()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "someoneelse",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/4.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);

        // act
        var result = await _controller.DeleteAuction(auctionEntity.Id);

        // assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_SaveSucceeds_ReturnsOk_AndPublishes()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "testuser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/5.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.DeleteAuction(auctionEntity.Id);

        // assert
        Assert.IsType<OkResult>(result);
        _publishEndpoint.Verify(p => p.Publish<AuctionDeleted>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _auctionRepo.Verify(r => r.RemoveAuction(auctionEntity), Times.Once);
    }

    [Fact]
    public async Task DeleteAuction_SaveFails_ReturnsBadRequest()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "testuser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/6.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // act
        var result = await _controller.DeleteAuction(auctionEntity.Id);

        // assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAuctions_WithDateParam_PassesDateToRepo()
    {
        // arrange
        var date = "2023-01-01";
        var auctions = _fixture.CreateMany<AuctionDto>(5).ToList();
        _auctionRepo.Setup(r => r.GetAuctionsAsync(date)).ReturnsAsync(auctions);

        // act
        var result = await _controller.GetAuctions(date);

        // assert
        Assert.Equal(5, result.Value.Count);
        _auctionRepo.Verify(r => r.GetAuctionsAsync(It.Is<string>(d => d == date)), Times.Once);
    }

    [Fact]
    public async Task GetAuctionsById_WithValidGuid_ReturnsAuctions()
    {
        // arrange

        var auction = _fixture.Create<AuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);

        // act
        var result = await _controller.GetAuctionById(auction.Id);

        // assert
        Assert.Equal(auction.Make, result.Value.Make);
        Assert.IsType<AuctionDto>(result.Value);
    }

    [Fact]
    public async Task GetAuctionsById_WithInvalidGuid_ReturnsNotFound()
    {
        // arrange
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(value: null);

        // act
        var result = await _controller.GetAuctionById(Guid.NewGuid());

        // assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuctions_WithValidCreateAuctionDto_ReturnsCreateAtAuction()
    {
        // arrange
        var auction = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(repo => repo.AddAuctionAsync(It.IsAny<Auction>()));
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.CreateAuction(auction);
        var createdResult = result.Result as CreatedAtActionResult;

        // assert
        Assert.NotNull(createdResult);
        Assert.Equal("GetAuctionById", createdResult?.ActionName);
        Assert.IsType<AuctionDto>(createdResult?.Value);
    }

    [Fact]
    public async Task CreateAuction_FailedSave_Returns400BadRequest()
    {
        // arrange
        var createDto = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(r => r.AddAuctionAsync(It.IsAny<Auction>()));
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // act
        var result = await _controller.CreateAuction(createDto);

        // assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "testuser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/7.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var updateDto = new UpdateAuctionDto { Make = "Honda", Model = "Civic", Year = 2021, Color = "Red", Mileage = 2000 };

        // act
        var result = await _controller.UpdateAuction(auctionEntity.Id, updateDto);

        // assert
        Assert.IsType<OkResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "otheruser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/8.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auctionEntity);

        // act
        var result = await _controller.UpdateAuction(auctionEntity.Id, new UpdateAuctionDto { Make = "Honda" });

        // assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
    {
        // arrange
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Auction)null);

        // act
        var result = await _controller.UpdateAuction(Guid.NewGuid(), new UpdateAuctionDto());

        // assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "testuser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/9.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);
        _auctionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // act
        var result = await _controller.DeleteAuction(auctionEntity.Id);

        // assert
        Assert.IsType<OkResult>(result);
        _auctionRepo.Verify(r => r.RemoveAuction(auctionEntity), Times.Once);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
    {
        // arrange
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Auction)null);

        // act
        var result = await _controller.DeleteAuction(Guid.NewGuid());

        // assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidUser_Returns403Response()
    {
        // arrange
        var auctionEntity = new Auction
        {
            Id = Guid.NewGuid(),
            Seller = "otheruser",
            Item = new Item { Make = "Toyota", Model = "Camry", Year = 2020, Color = "Blue", Mileage = 1000, ImageUrl = "http://img.example/10.jpg" }
        };
        _auctionRepo.Setup(r => r.GetAuctionEntityByIdAsync(auctionEntity.Id)).ReturnsAsync(auctionEntity);

        // act
        var result = await _controller.DeleteAuction(auctionEntity.Id);

        // assert
        Assert.IsType<ForbidResult>(result);
    }

}
