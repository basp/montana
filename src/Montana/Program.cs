using Montana;
using Montana.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder
        .Configuration
        .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MontanaDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapTimeEntryEndpoints();

app.Run();

public partial class Program;
