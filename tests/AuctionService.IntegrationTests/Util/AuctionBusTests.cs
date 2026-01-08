using System;
using AuctionService.Data;
using AuctionService.IntegrationTests.Fixtures;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Util;

public class AuctionBusTests : IClassFixture<CustomWebAppFactory>, IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _client;
    private ITestHarness _testHarness;
    public AuctionBusTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _testHarness = _factory.Services.GetTestHarness();
        
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
   
}
