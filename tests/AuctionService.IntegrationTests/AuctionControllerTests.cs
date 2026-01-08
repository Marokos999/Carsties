using System;
using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using Contracts;
using MassTransit.Testing;
using AuctionService.DTOs;
using Xunit;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("AuctionServiceIntegration")]
public class AuctionControllerTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _client;
    private const string BaseUrl = "api/auctions";

    public AuctionControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    public Task InitializeAsync() => Task.CompletedTask;

     public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<AuctionDbContext>();
        DBHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }

    private void SetUser(string user)
    {
        _client.DefaultRequestHeaders.Remove(TestAuthHandler.UserHeaderName);
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, user);
    }

    private static CreateAuctionDto BuildCreateDto()
    {
        var now = DateTime.UtcNow;
        return new CreateAuctionDto
        {
            Make = $"Make-{Guid.NewGuid():N}".Substring(0, 8),
            Model = $"Model-{Guid.NewGuid():N}".Substring(0, 8),
            Year = now.Year,
            Color = "Blue",
            Mileage = Random.Shared.Next(0, 200_000),
            ImageUrl = "https://example.com/img.jpg",
            ReservePrice = Random.Shared.Next(1000, 100000),
            AuctionEnd = now.AddDays(7)
        };
    }

    private static UpdateAuctionDto BuildUpdateDto() => new UpdateAuctionDto
    {
        Make = "UpdatedMake",
        Model = "UpdatedModel",
        Year = DateTime.UtcNow.Year,
        Color = "Red",
        Mileage = Random.Shared.Next(0, 200_000)
    };

    private async Task<AuctionDto?> CreateAuctionAsUserAsync(string user)
    {
        SetUser(user);
        var dto = BuildCreateDto();
        var res = await _client.PostAsJsonAsync(BaseUrl, dto);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<AuctionDto>();
    }

    [Fact]
    public async Task GetAuctions_ShouldIncludeNewlyCreated()
    {
        var before = await _client.GetFromJsonAsync<List<AuctionDto>>(BaseUrl) ?? [];

        var u = $"user-{Guid.NewGuid():N}";
        await CreateAuctionAsUserAsync(u);
        await CreateAuctionAsUserAsync(u);

        var after = await _client.GetFromJsonAsync<List<AuctionDto>>(BaseUrl) ?? [];
        Assert.True(after.Count >= before.Count + 2);
    }

    [Fact]
    public async Task GetAuctions_WithFutureDate_ShouldReturnEmpty()
    {
        var date = DateTime.UtcNow.AddYears(1).ToString("o");
        var list = await _client.GetFromJsonAsync<List<AuctionDto>>($"{BaseUrl}?date={Uri.EscapeDataString(date)}");
        Assert.NotNull(list);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task GetAuctionById_Existing_ShouldReturnAuction()
    {
        var user = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(user);
        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.NotNull(dto);
        Assert.Equal(created!.Id, dto!.Id);
    }

    [Fact]
    public async Task GetAuctionById_NotFound_ShouldReturn404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithValidDto_ShouldReturn201AndSeller()
    {
        var user = $"user-{Guid.NewGuid():N}";
        SetUser(user);
        var dto = BuildCreateDto();

        var response = await _client.PostAsJsonAsync(BaseUrl, dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.NotNull(created);
        Assert.Equal(user, created!.Seller);
        Assert.Equal(dto.Make, created.Make);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task CreateAuction_ShouldPublish_AuctionCreated_Message()
    {
        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        var user = $"user-{Guid.NewGuid():N}";
        SetUser(user);
        var dto = BuildCreateDto();

        var response = await _client.PostAsJsonAsync(BaseUrl, dto);
        response.EnsureSuccessStatusCode();

        var published = await harness.Published.Any<AuctionCreated>();
        Assert.True(published);
    }

    [Fact]
    public async Task UpdateAuction_NotFound_ShouldReturn404()
    {
        var user = $"user-{Guid.NewGuid():N}";
        SetUser(user);
        var update = new UpdateAuctionDto { Year = 2021, Color = "Green", Mileage = 1 };
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_AsSeller_ShouldReturn200()
    {
        var seller = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(seller);

        SetUser(seller);
        var update = BuildUpdateDto();
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_AsSeller_ShouldPublish_AuctionUpdated_Message()
    {
        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        var seller = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(seller);

        SetUser(seller);
        var update = BuildUpdateDto();
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var published = await harness.Published.Any<AuctionUpdated>();
        Assert.True(published);
    }

    [Fact]
    public async Task UpdateAuction_AsDifferentUser_ShouldReturn403()
    {
        var seller = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(seller);

        SetUser($"user-{Guid.NewGuid():N}");
        var update = BuildUpdateDto();
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuction_AsSeller_ShouldReturn200()
    {
        var seller = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(seller);

        SetUser(seller);
        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuction_AsSeller_ShouldPublish_AuctionDeleted_Message()
    {
        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        var seller = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(seller);

        SetUser(seller);
        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var published = await harness.Published.Any<AuctionDeleted>();
        Assert.True(published);
    }

    [Fact]
    public async Task DeleteAuction_AsDifferentUser_ShouldReturn403()
    {
        var seller = $"user-{Guid.NewGuid():N}";
        var created = await CreateAuctionAsUserAsync(seller);

        SetUser($"user-{Guid.NewGuid():N}");
        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuction_NotFound_ShouldReturn404()
    {
        var user = $"user-{Guid.NewGuid():N}";
        SetUser(user);
        var response = await _client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
