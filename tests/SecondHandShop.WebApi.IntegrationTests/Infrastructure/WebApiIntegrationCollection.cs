using Xunit;

namespace SecondHandShop.WebApi.IntegrationTests.Infrastructure;

[CollectionDefinition("WebApiIntegration")]
public sealed class WebApiIntegrationCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
