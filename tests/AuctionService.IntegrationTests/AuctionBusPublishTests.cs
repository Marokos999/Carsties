using System;
using System.Net;
using System.Net.Http.Json;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Contracts;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuctionService.IntegrationTests;

[Collection("AuctionServiceIntegration")]
public class AuctionBusPublishTests
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _client;
    private const string BaseUrl = "api/auctions";

    public AuctionBusPublishTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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

    [Fact]
    public async Task CreateAuction_ShouldPublish_AuctionCreated()
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
    public async Task UpdateAuction_AsSeller_ShouldPublish_AuctionUpdated()
    {
        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        var seller = $"user-{Guid.NewGuid():N}";
        SetUser(seller);
        var createRes = await _client.PostAsJsonAsync(BaseUrl, BuildCreateDto());
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<AuctionDto>();

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", BuildUpdateDto());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var published = await harness.Published.Any<AuctionUpdated>();
        Assert.True(published);
    }

    [Fact]
    public async Task DeleteAuction_AsSeller_ShouldPublish_AuctionDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        var seller = $"user-{Guid.NewGuid():N}";
        SetUser(seller);
        var createRes = await _client.PostAsJsonAsync(BaseUrl, BuildCreateDto());
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<AuctionDto>();

        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var published = await harness.Published.Any<AuctionDeleted>();
        Assert.True(published);
    }
}
