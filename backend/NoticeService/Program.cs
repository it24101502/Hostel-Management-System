using System.Data;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NoticeService.Jobs;
using NoticeService.Middleware;
using NoticeService.Options;
using NoticeService.Repositories;
using NoticeService.Services;
using Quartz;

// MySQL DATE columns are read as DateTime. This lets Dapper map them
// to and from DateOnly (used by the notice DTOs and the archive job).
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("NoticeArchivalJob");

    q.AddJob<NoticeArchivalJob>(opts => opts.WithIdentity(jobKey));

    // Every night at 00:00:00.
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("NoticeArchivalJob-trigger")
        .WithCronSchedule("0 0 0 * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NoticeService API", Version = "v1" });
});

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://20.212.39.185:5173",
                    "https://zealous-desert-0c41b8500.6.azurestaticapps.net")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
    jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("A secure JWT signing key is required.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                NameClaimType = JwtRegisteredClaimNames.UniqueName,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseMiddleware<RoleAuthorizationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => DateOnly.Parse(value.ToString()!)
    };
}