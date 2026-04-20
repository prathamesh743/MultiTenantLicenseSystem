using Hangfire;
using Hangfire.SqlServer;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ── DATABASE & MULTI-TENANCY CONFIGURATION ────────────────────────────────
// We use a custom factory for LicenseDbContext to inject the TenantId from the JWT.
// This ensures that every query executed by the context is automatically scoped to the current tenant.
builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        b => b.MigrationsAssembly("LicenseService")));

builder.Services.AddScoped<LicenseDbContext>(sp =>
{
    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    // Extract TenantId from JWT claims. Default to 'tenant1' for development if not present.
    var tenantId = httpContext?.User?.FindFirst("TenantId")?.Value ?? "tenant1";

    var options = sp.GetRequiredService<DbContextOptions<LicenseDbContext>>();
    var ctx = new LicenseDbContext(options);
    ctx.TenantId = tenantId;
    return ctx;
});

// ── CQRS & BACKGROUND JOBS ────────────────────────────────────────────────
// Register MediatR for CQRS (Command Query Responsibility Segregation) pattern.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Hangfire is used for background processing, such as license renewals or long-running tasks.
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = "LicenseSystem",
            ValidAudience = "LicenseSystem",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForAssignment2026_ThisIsLongEnoughForHS256_2026"))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
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

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");   


app.UseSwagger();
app.UseSwaggerUI();

RecurringJob.AddOrUpdate("renewal-job", () => Console.WriteLine("✅ Background renewal job executed for all tenants"), Cron.Daily);

app.MapControllers();

app.Run();