using System.Security.Claims;
using System.Text;
using AccommodationService.Middleware;
using AccommodationService.Options;
using AccommodationService.Repositories;
using AccommodationService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "https://zealous-desert-0c41b8500.6.azurestaticapps.net")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
    jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException(
        "A secure JWT signing key is required.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Key)),

                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                NameClaimType =
                    JwtRegisteredClaimNames.UniqueName,

                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomAuditRepository, RoomAuditRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IRoomAllocationRepository,RoomAllocationRepository>();
builder.Services.AddScoped<IRoomAllocationService,RoomAllocationService>();

var app = builder.Build();

app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseMiddleware<RoleAuthorizationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();