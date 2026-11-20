namespace Montana.Entities;

public class Project
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe identifier, derived from <see cref="Name"/> by the DbContext
    /// and kept unique.
    /// </summary>
    public string Slug { get; internal set; } = string.Empty;

    public string? ColorHex { get; set; }

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
