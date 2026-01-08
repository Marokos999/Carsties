using Xunit;

namespace AuctionService.IntegrationTests.Fixtures;

// Shared collection fixture to reuse the same CustomWebAppFactory and DB
[CollectionDefinition("AuctionServiceIntegration")]
public class AuctionServiceIntegrationCollection : ICollectionFixture<CustomWebAppFactory>
{
    // No code needed; this class serves as the collection definition
}
