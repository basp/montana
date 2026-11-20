namespace Montana;

using Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

public class MontanaDbContext(DbContextOptions<MontanaDbContext> options)
    : DbContext(options)
{
    public DbSet<TimeEntry> TimeEntries => this.Set<TimeEntry>();

    public DbSet<Project> Projects => this.Set<Project>();

    public DbSet<Tag> Tags => this.Set<Tag>();

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project
                .Property(p => p.Name)
                .HasMaxLength(100);
            project
                .Property(p => p.Slug)
                .HasMaxLength(100)
                .UseCollation("NOCASE");
            project
                .Property(p => p.ColorHex)
                .HasMaxLength(9);
            project
                .HasIndex(p => p.Slug)
                .IsUnique();
        });

        modelBuilder.Entity<Tag>(tag =>
        {
            tag
                .Property(t => t.Name)
                .HasMaxLength(50);
            tag
                .Property(t => t.Slug)
                .HasMaxLength(50)
                .UseCollation("NOCASE");
            tag
                .Property(t => t.ColorHex)
                .HasMaxLength(9);
            tag
                .HasIndex(t => t.Slug)
                .IsUnique();
        });

        modelBuilder.Entity<TimeEntry>(entry =>
        {
            // Time entry invariant name.
            const string teCk = 
                "CK_TimeEntries_EndedAt_Not_Before_StartedAt";
            
            // Invariant: ensure ended after started.
            const string teCkSql = 
                "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"";            

            entry.Property(e => e.Description).HasMaxLength(2048);
            entry.HasIndex(e => e.StartedAt);
            entry.HasIndex(e => e.ProjectId);
            entry.ToTable(table => table.HasCheckConstraint(teCk, teCkSql));
            entry.HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
            entry.HasMany(e => e.Tags)
                .WithMany(t => t.TimeEntries)
                .UsingEntity(join => join.ToTable("TimeEntryTags"));
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.PrepareChanges();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void PrepareChanges()
    {
        var reservedProjectSlugs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var reservedTagSlugs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in this.ChangeTracker.Entries<TimeEntry>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var timeEntry = entry.Entity;
            if (entry.State is EntityState.Added && timeEntry.CreatedAt == default)
            {
                timeEntry.CreatedAt = DateTimeOffset.UtcNow;
            }

            timeEntry.DurationSeconds = timeEntry.EndedAt switch
            {
                { } endedAt when endedAt < timeEntry.StartedAt =>
                    throw new InvalidOperationException(
                        $"TimeEntry {timeEntry.Id} ends ({endedAt:O}) before it starts ({timeEntry.StartedAt:O})."),
                { } endedAt =>
                    (long)(endedAt - timeEntry.StartedAt).TotalSeconds,
                _ => null,
            };
        }

        foreach (var entry in this.ChangeTracker.Entries<Project>())
        {
            if (entry.State is EntityState.Added ||
                (entry.State is EntityState.Modified &&
                 entry.Property(p => p.Name).IsModified))
            {
                entry.Entity.Slug = NextFreeSlug(
                    Slug.From(entry.Entity.Name),
                    reservedProjectSlugs,
                    slug => 
                        this.Set<Project>().Any(
                            p => p.Slug == slug && p.Id != entry.Entity.Id));
            }
        }

        foreach (var entry in this.ChangeTracker.Entries<Tag>())
        {
            if (entry.State is EntityState.Added
                || (entry.State is EntityState.Modified && entry.Property(t => t.Name).IsModified))
            {
                entry.Entity.Slug = NextFreeSlug(
                    Slug.From(entry.Entity.Name),
                    reservedTagSlugs,
                    slug => Set<Tag>().Any(t => t.Slug == slug && t.Id != entry.Entity.Id));
            }
        }
    }

    private static string NextFreeSlug(
        string baseSlug,
        HashSet<string> reserved,
        Func<string, bool> alreadyTaken)
    {
        var candidate = baseSlug;
        var suffix = 2;

        while (reserved.Contains(candidate) || alreadyTaken(candidate))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        reserved.Add(candidate);
        return candidate;
    }
}
