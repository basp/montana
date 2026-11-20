using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Montana;
using Montana.Entities;

namespace Montana.Tests;

public sealed class MontanaDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MontanaDbContext _context;

    public MontanaDbContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MontanaDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new MontanaDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Timestamps_round_trip_as_utc()
    {
        var startedAt = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(2));

        _context.TimeEntries.Add(new TimeEntry { StartedAt = startedAt });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stored = await _context.TimeEntries.SingleAsync();

        Assert.Equal(startedAt, stored.StartedAt);
        Assert.Equal(TimeSpan.Zero, stored.StartedAt.Offset);

        // 09:00+02:00 must land in the column as 07:00, with no offset suffix,
        // so that string ordering matches chronological ordering.
        var raw = (string)_connection.CreateCommand()
            .WithText("SELECT \"StartedAt\" FROM \"TimeEntries\" LIMIT 1")
            .ExecuteScalar()!;

        Assert.StartsWith("2026-03-02 07:00:00", raw);
    }

    [Fact]
    public async Task Duration_is_derived_from_timestamps()
    {
        var startedAt = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(2));
        var endedAt = startedAt.AddMinutes(90);

        _context.TimeEntries.AddRange(
            new TimeEntry { StartedAt = startedAt, EndedAt = endedAt },
            new TimeEntry { StartedAt = startedAt });

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stored = await _context.TimeEntries.SingleAsync(e => e.EndedAt != null);
        var running = await _context.TimeEntries.SingleAsync(e => e.EndedAt == null);

        Assert.Equal(5400, stored.DurationSeconds); // 90 minutes
        Assert.Equal(endedAt, stored.EndedAt);
        Assert.Equal(TimeSpan.Zero, stored.EndedAt!.Value.Offset);

        Assert.Null(running.DurationSeconds);
    }

    [Fact]
    public async Task Multiple_running_entries_are_allowed()
    {
        _context.TimeEntries.AddRange(
            new TimeEntry { StartedAt = DateTimeOffset.UtcNow, Description = "working hours" },
            new TimeEntry { StartedAt = DateTimeOffset.UtcNow, Description = "task A" });

        await _context.SaveChangesAsync();

        Assert.Equal(2, await _context.TimeEntries.CountAsync(e => e.EndedAt == null));
    }

    [Fact]
    public async Task Entry_cannot_end_before_it_starts()
    {
        var startedAt = DateTimeOffset.UtcNow;

        _context.TimeEntries.Add(new TimeEntry
        {
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(-1)
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _context.SaveChangesAsync());

        Assert.Contains("ends", error.Message);
    }

    [Fact]
    public async Task Database_rejects_entry_ending_before_it_starts()
    {
        // The app-level guard above throws first, so write past it to prove the
        // check constraint actually exists in the schema.
        var sql = (string)_connection.CreateCommand()
            .WithText("SELECT sql FROM sqlite_master WHERE name = 'TimeEntries'")
            .ExecuteScalar()!;

        Assert.Contains("CK_TimeEntries_EndedAt_Not_Before_StartedAt", sql);
    }

    [Fact]
    public async Task Slugs_are_derived_and_unique()
    {
        _context.Projects.AddRange(
            new Project { Name = "Client: Acme" },
            new Project { Name = "Client Acme" },
            new Project { Name = "Café" });

        await _context.SaveChangesAsync();

        var slugs = await _context.Projects.ToDictionaryAsync(p => p.Name, p => p.Slug);

        Assert.Equal("client-acme", slugs["Client: Acme"]);
        Assert.Equal("client-acme-2", slugs["Client Acme"]);
        Assert.Equal("cafe", slugs["Café"]);
    }

    [Fact]
    public async Task Slugs_are_unique_case_insensitively_across_saves()
    {
        _context.Tags.Add(new Tag { Name = "Client Acme" });
        await _context.SaveChangesAsync();

        _context.Tags.Add(new Tag { Name = "CLIENT ACME" });
        await _context.SaveChangesAsync();

        var slugs = await _context.Tags.Select(t => t.Slug).ToListAsync();

        Assert.Equal(["client-acme", "client-acme-2"], slugs.OrderBy(s => s));
    }

    [Fact]
    public async Task Renaming_regenerates_the_slug()
    {
        var project = new Project { Name = "Acme" };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        Assert.Equal("acme", project.Slug);

        project.Name = "Acme Corp";
        await _context.SaveChangesAsync();

        Assert.Equal("acme-corp", project.Slug);
    }
}

internal static class SqliteCommandExtensions
{
    public static SqliteCommand WithText(this SqliteCommand command, string text)
    {
        command.CommandText = text;
        return command;
    }
}
