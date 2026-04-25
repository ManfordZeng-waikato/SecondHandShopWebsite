using FluentAssertions;

namespace SecondHandShop.WebApi.IntegrationTests.RealStack;

public sealed class RealStackWebApplicationFactoryTests
{
    [Fact]
    public void CreateClient_ShouldUseConfiguredConnectionStringDuringStartupSeeding()
    {
        using var factory = new RealStackWebApplicationFactory
        {
            ConnectionString = "Host=127.0.0.1;Port=6543;Database=missing;Username=postgres;Password=postgres"
        };

        var act = () => factory.CreateClient();

        var exception = act.Should().Throw<Exception>().Which;
        GetExceptionMessages(exception).Should().Contain(message => message.Contains("127.0.0.1:6543"));
    }

    private static IEnumerable<string> GetExceptionMessages(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current.Message;
        }
    }
}