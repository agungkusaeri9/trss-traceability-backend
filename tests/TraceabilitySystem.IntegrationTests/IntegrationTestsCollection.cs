using Xunit;

namespace TraceabilitySystem.IntegrationTests;

[CollectionDefinition("Integration Tests Collection")]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
