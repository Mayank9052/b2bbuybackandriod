using B2BBuyback.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IBuybackService, InMemoryBuybackService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "BuybackClients",
        policy =>
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

            if (origins is { Length: > 0 })
            {
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                return;
            }

            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseCors("BuybackClients");

app.MapControllers();
app.MapGet(
    "/",
    () => Results.Ok(
        new
        {
            name = "B2B Buyback API",
            status = "running",
            endpoints = new[]
            {
                "/api/health",
                "/api/dashboard/summary",
                "/api/buybacks"
            }
        }));

app.Run();
