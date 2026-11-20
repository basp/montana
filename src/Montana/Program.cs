using Montana;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MontanaDbContext>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();