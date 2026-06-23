//#if (useJwt)
using System.Text;
//#endif
//#if (useRateLimiting)
using System.Threading.RateLimiting;
//#endif
//#if (useMediatr)
using Application.Extensions;
//#endif
using Infrastructure.Extensions;
//#if (useJwt)
using Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
//#endif
//#if (useApiVersioning)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
//#endif
//#if (useRateLimiting)
using Microsoft.AspNetCore.RateLimiting;
//#endif
//#if (useJwt)
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
//#endif
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
//#if (useApiVersioning)
using Swashbuckle.AspNetCore.SwaggerGen;
//#endif
using Web.Middleware;
using Web.Setup;

// ======================== Serilog ========================
var date = DateTime.Now.ToString("yyyy-MM-dd");
var time = DateTime.Now.ToString("HH-mm-ss");
var logPath = Path.Combine("Logs", date, time);
var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(isDev ? LogEventLevel.Debug : LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: SerilogTheme.Colored)
    .WriteTo.Logger(lc => lc
        .Filter.With(SourceContextFilter.Exclude("Api"))
        .WriteTo.File(
            path: Path.Combine(logPath, "app.log"),
            outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"))
    .WriteTo.Logger(lc => lc
        .Filter.With(SourceContextFilter.Include("Api"))
        .WriteTo.File(
            path: Path.Combine(logPath, "api.log"),
            outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{StatusCode}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

//#if (useJwt)
// ======================== Options ========================
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
//#endif

// ======================== Infrastructure & Application ========================
var useTestDb = builder.Environment.IsDevelopment();
builder.Services.AddInfrastructure(builder.Configuration, useTestDb);
//#if (useMediatr)
builder.Services.AddApplication();
//#endif

//#if (useJwt)
// ======================== JWT Authentication ========================
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt configuration is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiErrors.Unauthorized());
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiErrors.Forbidden());
            }
        };
    });

// ======================== Authorization ========================
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Default", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
//#endif

//#if (useRazorPages)
// ======================== Razor Pages ========================
builder.Services.AddRazorPages();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
//#endif

// ======================== Controllers ========================
builder.Services.AddControllers();

//#if (useApiVersioning)
// ======================== API Versioning ========================
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
//#endif

// ======================== Swagger / OpenAPI ========================
builder.Services.AddEndpointsApiExplorer();
//#if (useApiVersioning)
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
//#endif
builder.Services.AddSwaggerGen();

//#if (useRateLimiting)
// ======================== Rate Limiting ========================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiErrors.RateLimitExceeded(), cancellationToken);
    };

    options.AddPolicy("Api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
//#endif

builder.Services.AddProblemDetails();

// ======================== App Build ========================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//#if (useApiVersioning)
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
//#endif

app.UseSwagger();
//#if (useApiVersioning)
app.UseSwaggerUI(options =>
{
    foreach (var desc in apiVersionProvider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{desc.GroupName}/swagger.json",
            desc.GroupName.ToUpperInvariant());
    }
});
//#else
app.UseSwaggerUI();
//#endif
app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
});

app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<RequestLoggingMiddleware>();
//#if (useRateLimiting)
app.UseRateLimiter();
//#endif
//#if (useJwt)
app.UseAuthentication();
app.UseAuthorization();
//#endif

app.MapControllers();
//#if (useRazorPages)
app.MapRazorPages();
//#endif

try
{
    Log.Information("Приложение запущено");
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
