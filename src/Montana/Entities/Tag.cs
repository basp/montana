namespace Montana.Entities;

public class Tag
{
    public Guid Id { get; private set; } =
        Guid.CreateVersion7();

    public string Slug { get; private set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string? ColorHex { get; set; }

    public ICollection<TimeEntry> TimeEntries { get; set; } =
        new List<TimeEntry>();
}
