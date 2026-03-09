using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using IlkProjem.DAL.Data;
using IlkProjem.BLL.Services;
using IlkProjem.DAL.Repositories;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Threading.RateLimiting;
using IlkProjem.Core.Interfaces;
using IlkProjem.DAL.Interceptors;
using IlkProjem.API.Middlewares;
using Serilog;
using FluentValidation;
using IlkProjem.BLL.Interfaces;
using IlkProjem.DAL.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOCALIZATION SETUP ---
builder.Services.AddLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { "en-US", "tr-TR" };
    options.DefaultRequestCulture = new RequestCulture("tr-TR"); // Architect's preference
    options.SupportedCultures = cultures.Select(c => new CultureInfo(c)).ToList();
    options.SupportedUICultures = cultures.Select(c => new CultureInfo(c)).ToList();

    // Prioritize Accept-Language header over QueryStrings
    options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
});

// --- 2. OPENAPI / SWAGGER SETUP ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Standard Swagger
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        var localizer = context.ApplicationServices.GetRequiredService<IStringLocalizer<Program>>();
        document.Info.Title = localizer["ApiTitle"];
        return Task.CompletedTask;
    });
});

// --- 3. DATABASE & SERVICES (BLL/DAL) ---
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true
    };
});

// --- 4. RATE LIMITING ---
builder.Services.AddRateLimiter(options =>
{
    // --- A. TOPLAM (GLOBAL) LIMIT ---
    // Uygulamaya saniyede toplam 100'den fazla istek gelmesin (Sunucu sağlığı için)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter("Global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100, 
            Window = TimeSpan.FromSeconds(1),
            QueueLimit = 0
        }));

   // --- B. KULLANICI BAZLI (PER USER) LIMIT ---
    // Her bir IP adresi dakikada 20 istek atabilsin
   options.AddPolicy("PerUser", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    
    options.RejectionStatusCode = 429; // Too Many Requests
});

builder.Services.AddHttpContextAccessor(); // HttpContext'e erişim için şart

// Dependency Injection matching your Business/DataAccess layers
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IFilesService, FilesService>();
builder.Services.AddScoped<IFilesRepository, FilesRepository>();
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IHouseRepository, HouseRepository>();
builder.Services.AddScoped<IHouseService, HouseService>();
builder.Services.AddScoped<IMailService, MailManager>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- FluentValidation: Register all validators from BLL assembly ---
builder.Services.AddValidatorsFromAssemblyContaining<IlkProjem.BLL.ValidationRules.FluentValidation.CustomerDtoValidators.CustomerCreateDtoValidator>();

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// --- 5. CORS (Allowing Angular SPA) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()); // HttpOnly cookie'ler için gerekli
});

// --- 6. SERILOG SETUP (PostgreSQL'e loglama) ---

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.PostgreSQL(builder.Configuration.GetConnectionString("DefaultConnection"), "ServiceLog")
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// --- 7. MIDDLEWARE PIPELINE ("Interceptors") ---
// This acts as the server-side interceptor for Content Language
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- 8. STATIC FILE SERVING (Yüklenen dosyalar için) ---
var storagePath = builder.Configuration["FileSettings:StoragePath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

if (!Directory.Exists(storagePath))
    Directory.CreateDirectory(storagePath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(storagePath),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseCors("AllowAngular");
app.UseMiddleware<LoggingMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();    // "Sen kimsin?" (JWT kontrolü)
app.UseAuthorization();     // "Bunu görmeye yetkin var mı?"

app.UseHttpsRedirection();
app.MapControllers().RequireRateLimiting("PerUser");



// --- 9. SEED DATA ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!context.Customers.Any())
    {
        context.Customers.AddRange(CustomerSeeder.GetFakeCustomers(50));
        context.SaveChanges();
    }
}

app.MapOpenApi();
app.Run("http://localhost:5005");