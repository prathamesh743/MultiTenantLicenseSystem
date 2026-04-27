using Hangfire;
using Hangfire.SqlServer;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Data;
using SharedKernel.Security;
using SharedKernel.Tenancy;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ── DATABASE & MULTI-TENANCY CONFIGURATION ────────────────────────────────
// We use a custom factory for LicenseDbContext to inject the TenantId from the JWT.
// This ensures that every query executed by the context is automatically scoped to the current tenant.
builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        b => b.MigrationsAssembly("SharedKernel")));

builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

builder.Services.AddScoped<LicenseDbContext>(sp =>
{
    // For authenticated requests we require TenantId claim; unauthenticated endpoints must use IgnoreQueryFilters().
    var tenantProvider = sp.GetRequiredService<ITenantProvider>();
    var tenantId = tenantProvider.TryGetTenantId() ?? "__unauth__";

    var options = sp.GetRequiredService<DbContextOptions<LicenseDbContext>>();
    var ctx = new LicenseDbContext(options);
    ctx.TenantId = tenantId;
    return ctx;
});

builder.Services.AddSingleton<ILicenseDbContextTenantFactory, LicenseDbContextTenantFactory>();

// ── CQRS & BACKGROUND JOBS ────────────────────────────────────────────────
// Register MediatR for CQRS (Command Query Responsibility Segregation) pattern.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Hangfire is used for background processing, such as license renewals or long-running tasks.
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<RenewalJob>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    throw new InvalidOperationException("JWT key missing. Set Jwt:Key via configuration or env var Jwt__Key.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "LicenseService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter token here (No need to prefix with 'Bearer ')"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAdminOnlyAuthorizationFilter() }
});

using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var autoMigrate = config.GetValue("Database:AutoMigrate", defaultValue: app.Environment.IsDevelopment());
    if (autoMigrate)
    {
        var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
        db.Database.Migrate();
    }

    AdminSeeder.Seed(scope.ServiceProvider, app.Environment);
}

app.UseSwagger();
app.UseSwaggerUI();

RecurringJob.AddOrUpdate<RenewalJob>(
    "renewal-job",
    job => job.RunAsync(CancellationToken.None),
    Cron.Daily);

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
