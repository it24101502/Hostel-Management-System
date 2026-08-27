using IdentityService.Repositories;
using IdentityService.Services;
using IdentityService.Middleware;
using IdentityService.Options;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<LockoutOptions>(
    builder.Configuration.GetSection(
        LockoutOptions.SectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
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

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<
    IAdminUserRepository,
    AdminUserRepository>();

builder.Services.AddScoped<
    IAdminUserService,
    AdminUserService>();

builder.Services.AddScoped<
    IGuardianContactRepository,
    GuardianContactRepository>();

builder.Services.AddScoped<
    IGuardianContactService,
    GuardianContactService>();

builder.Services.AddScoped<
    IProfileUniquenessRepository,
    ProfileUniquenessRepository>();

builder.Services.AddScoped<
    IProfileUniquenessService,
    ProfileUniquenessService>();

builder.Services.AddScoped<
    IStudentProfileRepository,
    StudentProfileRepository>();

builder.Services.AddScoped<
    IStudentProfileService,
    StudentProfileService>();

builder.Services.AddScoped<
    IFeeInvoiceRepository,
    FeeInvoiceRepository>();

builder.Services.AddScoped<
    IFeeInvoiceService,
    FeeInvoiceService>();

builder.Services.AddScoped<
    ILoginAuditRepository,
    LoginAuditRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<
    IJwtTokenService,
    JwtTokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseMiddleware<RoleAuthorizationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();