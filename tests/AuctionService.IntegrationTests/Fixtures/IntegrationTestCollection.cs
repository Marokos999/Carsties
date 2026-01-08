using Xunit;

namespace AuctionService.IntegrationTests.Fixtures;

[CollectionDefinition("AuctionServiceIntegration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebAppFactory>
{
}
