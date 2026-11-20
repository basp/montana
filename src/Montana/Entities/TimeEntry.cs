namespace Montana.Entities;

public class TimeEntry
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string? Description { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public long? DurationSeconds { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public ICollection<Tag> Tags { get; set; } =
        new List<Tag>();
}