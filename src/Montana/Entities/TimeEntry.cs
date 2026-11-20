namespace Montana.Entities;

public class TimeEntry
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string? Description { get; set; }

    /// <summary>The moment the entry started. Stored as UTC.</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// The moment the entry stopped, or <c>null</c> while it is running.
    /// Multiple entries may be running at once; that is left to the client.
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// Elapsed seconds, derived from the timestamps by the DbContext on save.
    /// </summary>
    public long? DurationSeconds { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; } = DateTimeOffset.UtcNow;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
