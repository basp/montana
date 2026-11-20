using Montana;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MontanaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Montana")));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
