using System.Net;

namespace Montana.Tests;

public sealed class ApplicationSmokeTests
{
    [Fact]
    public async Task App_hosts_and_answers_the_root_route()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
    }
}
