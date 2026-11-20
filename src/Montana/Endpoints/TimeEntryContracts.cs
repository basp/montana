namespace Montana.Endpoints;

public sealed record CreateTimeEntryRequest(string? Description);

public sealed record TimeEntryResponse(
    Guid Id,
    string? Description,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    long? DurationSeconds);
