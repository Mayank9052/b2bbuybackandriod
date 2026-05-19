using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.Data;
using OfficeOpenXml;
using B2BBuyback.Api.ModelBinders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using B2BBuyback.Api.Interfaces;
using B2BBuyback.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// EPPlus License
ExcelPackage.License.SetNonCommercialPersonal("B2BBuyback.Api");

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddScoped<IExchangeEmailService, ExchangeEmailService>();
// ── Controllers ───────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new DateOnlyModelBinderProvider());
});

// ── JWT Authentication ────────────────────────────────────────
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

// ── Swagger ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "B2B Buyback API",
        Version = "v1"
    });
});

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://34.203.61.70",
                "http://34.203.61.70:80",
                "http://34.203.61.70:443"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ── Explicit WebRootPath ──────────────────────────────────────
var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
    Directory.CreateDirectory(wwwrootPath);
builder.Environment.WebRootPath = wwwrootPath;

var app = builder.Build();

// ── Swagger ───────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "B2B Buyback API v1");
        options.RoutePrefix = "swagger";
    });
}

// ── Serve React build (wwwroot) ───────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();   // serves wwwroot — React build + /ExchangeImages inside wwwroot

// ── ALSO serve ExchangeImages from its physical disk location ─
// If images were saved outside wwwroot (e.g. in a temp folder or
// directly under ContentRoot), this second provider catches them.
//
// The controller saves to: GetWebRoot() / ExchangeImages / {id} / {file}
// GetWebRoot() tries wwwroot first, then ContentRoot/wwwroot, then temp.
// We register all three possible roots so images are always found.

var possibleRoots = new[]
{
    wwwrootPath,                                                        // primary
    builder.Environment.ContentRootPath,                                // fallback 1
    Path.Combine(Path.GetTempPath(), "bgauss-uploads"),                 // fallback 2
};

foreach (var root in possibleRoots)
{
    var exchangeImagesFolder = Path.Combine(root, "ExchangeImages");

    // Create the folder if it doesn't exist yet (avoids DirectoryNotFoundException)
    if (!Directory.Exists(exchangeImagesFolder))
    {
        try { Directory.CreateDirectory(exchangeImagesFolder); }
        catch { /* ignore — will be created on first upload */ }
    }

    // Register a static file provider for this root so /ExchangeImages/... URLs work
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(root),
        RequestPath  = "",           // serve at root — /ExchangeImages/11/Front.jpg
        ServeUnknownFileTypes = true, // allow .jpg .png without explicit MIME
        OnPrepareResponse = ctx =>
        {
            // Cache images for 1 hour in the browser
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600";
        }
    });
}

// ── CORS ──────────────────────────────────────────────────────
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── React routing fallback ────────────────────────────────────
app.MapFallbackToFile("index.html");

app.Run();