using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.Data;
using OfficeOpenXml;
using B2BBuyback.Api.Conventions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using B2BBuyback.Api.Interfaces;
using B2BBuyback.Api.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// EPPlus
ExcelPackage.License.SetNonCommercialPersonal("B2BBuyback.Api");

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ── Services ──────────────────────────────────────────────────
//builder.Services.AddScoped<IExchangeEmailService, ExchangeEmailService>();
builder.Services.AddScoped<IDealerOtpEmailService, DealerOtpEmailService>();
builder.Services.AddScoped<IExchangeEmailService, ExchangeEmailService>();
//builder.Services.AddScoped<IExchangeEmailService, ExchangeEmailService>();

// ── Controllers ───────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger convention to hide IFormFile endpoints
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    options.Conventions.Add(new HideFormFileEndpointsConvention());
});

// ── JWT Authentication ─────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
        };
    });

// ── Swagger ────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "B2B Buyback API", Version = "v1" });
    c.MapType<string>(() => new OpenApiSchema { Type = "string" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    c.ResolveConflictingActions(a => a.First());
});

// ── CORS ───────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://34.203.61.70",
                "http://34.203.61.70:80",
                "http://34.203.61.70:443")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ── WebRoot ────────────────────────────────────────────────────
var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath)) Directory.CreateDirectory(wwwrootPath);
builder.Environment.WebRootPath = wwwrootPath;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "B2B Buyback API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

foreach (var root in new[] {
    wwwrootPath,
    builder.Environment.ContentRootPath,
    Path.Combine(Path.GetTempPath(), "bgauss-uploads")
})
{
    var folder = Path.Combine(root, "ExchangeImages");
    try { Directory.CreateDirectory(folder); } catch { }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(root),
        RequestPath  = "",
        ServeUnknownFileTypes = true,
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600"
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();