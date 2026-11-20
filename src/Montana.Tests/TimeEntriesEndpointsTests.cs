using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Montana.Endpoints;
using Montana.Entities;

namespace Montana.Tests;

public sealed class TimeEntriesEndpointsTests
{
    [Fact]
    public async Task Post_starts_a_running_entry()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();
        var before = DateTimeOffset.UtcNow;

        var response = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = "start the timer" });

        var body = await response.Content
            .ReadFromJsonAsync<TimeEntryResponse>();

        // Assert the response body.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("start the timer", body.Description);
        Assert.Equal(TimeSpan.Zero, body.StartedAt.Offset);
        Assert.InRange(
            body.StartedAt,
            before.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Null(body.EndedAt);
        Assert.Null(body.DurationSeconds);
        Assert.Equal(
            $"/time-entries/{body.Id}",
            response.Headers.Location?.OriginalString);

        // Assert the database context directly.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MontanaDbContext>();
        var stored = await db.TimeEntries
            .AsNoTracking()
            .SingleAsync(e => e.Id == body.Id);

        Assert.Equal("start the timer", stored.Description);
        Assert.Null(stored.EndedAt);
        Assert.Null(stored.DurationSeconds);
    }

    [Fact]
    public async Task Post_trims_the_description()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = "  write the docs  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<TimeEntryResponse>();

        Assert.Equal("write the docs", body?.Description);
    }

    [Fact]
    public async Task Post_treats_a_blank_description_as_null()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = "   " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<TimeEntryResponse>();

        Assert.Null(body?.Description);
    }

    [Fact]
    public async Task Post_rejects_an_overlong_description_without_creating_a_row()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = new string('x', 2049) });

        // Assert response.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        // Assert body.
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.True(
            document.RootElement
                .GetProperty("errors")
                .TryGetProperty("description", out _));

        // Assert database context directly.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MontanaDbContext>();
        Assert.Equal(0, await db.TimeEntries.CountAsync());
    }

    [Fact]
    public async Task Post_leaves_an_existing_running_entry_untouched()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = "first" });
        var second = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = "second" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var firstBody = await first.Content.ReadFromJsonAsync<TimeEntryResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<TimeEntryResponse>();

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.NotEqual(firstBody.Id, secondBody.Id);
        Assert.Null(firstBody.EndedAt);
        Assert.Null(secondBody.EndedAt);

        var readBack = await client.GetFromJsonAsync<TimeEntryResponse>(
            $"/time-entries/{firstBody.Id}");

        Assert.NotNull(readBack);
        Assert.Equal("first", readBack.Description);
        Assert.Equal(firstBody.StartedAt, readBack.StartedAt);
        Assert.Null(readBack.EndedAt);
        Assert.Null(readBack.DurationSeconds);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MontanaDbContext>();
        Assert.Equal(
            2,
            await db.TimeEntries.CountAsync(e => e.EndedAt == null));
    }

    [Fact]
    public async Task Create_and_read_return_the_same_representation_without_a_url()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync(
            "/time-entries",
            new { description = "same shape" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var createdJson = JsonDocument.Parse(
            await created.Content.ReadAsStringAsync());

        var createdFields = createdJson.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            ["id", "description", "startedAt", "endedAt", "durationSeconds"],
            createdFields);
        Assert.DoesNotContain("url", createdFields);

        var location = created.Headers.Location!.OriginalString;
        var read = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        using var readJson = JsonDocument.Parse(
            await read.Content.ReadAsStringAsync());

        Assert.Equal(
            createdJson.RootElement.GetRawText(),
            readJson.RootElement.GetRawText());
    }

    [Fact]
    public async Task Get_returns_an_existing_entry()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var entry = await Seed(
            factory,
            new TimeEntry
            {
                Description = "read the entry back",
                StartedAt = DateTimeOffset.UtcNow,
            });

        var response = await client.GetAsync($"/time-entries/{entry.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TimeEntryResponse>();

        Assert.NotNull(body);
        Assert.Equal(entry.Id, body.Id);
        Assert.Equal(entry.Description, body.Description);
        Assert.Equal(entry.StartedAt, body.StartedAt);
        Assert.Null(body.EndedAt);
        Assert.Null(body.DurationSeconds);
    }

    [Fact]
    public async Task Get_returns_404_for_an_unknown_id()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/time-entries/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_returns_404_for_a_malformed_id()
    {
        using var factory = new MontanaWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/time-entries/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<TimeEntry> Seed(
        MontanaWebApplicationFactory factory,
        TimeEntry entry)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MontanaDbContext>();

        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        return entry;
    }
}
