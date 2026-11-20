namespace Montana.Endpoints;

using Entities;
using Microsoft.EntityFrameworkCore;

public static class TimeEntriesEndpoints
{
    public static IEndpointRouteBuilder MapTimeEntryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/time-entries");

        group.MapPost("/", CreateTimeEntry);

        group.MapGet("/{id:guid}", GetTimeEntry)
            .WithName("GetTimeEntry");

        return endpoints;
    }

    private static async Task<IResult> CreateTimeEntry(
        CreateTimeEntryRequest request,
        MontanaDbContext db,
        LinkGenerator links)
    {
        var description = request.Description?.Trim();

        if (description?.Length > 2048)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["description"] =
                        ["Description must be 2048 characters or fewer."],
                });
        }

        var entry = new TimeEntry
        {
            Description = string.IsNullOrEmpty(description) ? null : description,
            StartedAt = DateTimeOffset.UtcNow,
        };

        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        var location = links.GetPathByName("GetTimeEntry", new { id = entry.Id });

        return Results.Created(location!, ToResponse(entry));
    }

    private static async Task<IResult> GetTimeEntry(
        Guid id,
        MontanaDbContext db)
    {
        var entry = await db.TimeEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id);

        return entry is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Time entry not found")
            : Results.Ok(ToResponse(entry));
    }

    private static TimeEntryResponse ToResponse(TimeEntry entry) =>
        new(
            entry.Id,
            entry.Description,
            entry.StartedAt,
            entry.EndedAt,
            entry.DurationSeconds);
}
