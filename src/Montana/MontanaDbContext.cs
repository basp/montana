namespace Montana;

using Entities;
using Microsoft.EntityFrameworkCore;

public class MontanaDbContext(DbContextOptions options) 
    : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    
    public DbSet<TimeEntry> TimeEntries { get; set; }
    
    public DbSet<Tag> Tags { get; set; }
}