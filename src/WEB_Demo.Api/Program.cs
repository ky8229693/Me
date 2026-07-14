using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Bootstrap;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Endpoints;
using WEB_Demo.Api.E2E;
using WEB_Demo.Api.Middleware;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var corsOptions = builder.Configuration
    .GetSection(ApiCorsOptions.SectionName)
    .Get<ApiCorsOptions>() ?? new ApiCorsOptions();
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services.Configure<ApiCorsOptions>(
    builder.Configuration.GetSection(ApiCorsOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<IdentityUser>>();
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenStamp = context.Principal?.FindFirstValue(AppClaimTypes.SecurityStamp);
                var user = userId is null ? null : await userManager.FindByIdAsync(userId);

                if (user is null || await userManager.IsLockedOutAsync(user) ||
                    string.IsNullOrEmpty(tokenStamp) ||
                    !string.Equals(user.SecurityStamp, tokenStamp, StringComparison.Ordinal))
                {
                    context.Fail("The token is no longer valid.");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail("Authentication is required.", ApiErrorCodes.Unauthorized));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail("You do not have permission to access this resource.", ApiErrorCodes.Forbidden));
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.AdminOnly, policy =>
        policy.RequireRole(AppRoles.Admin));
});
builder.Services.AddSingleton<ITodoService, InMemoryTodoService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AppRoleInitializer>();
builder.Services.AddScoped<AdminBootstrapService>();
builder.Services.AddScoped<AccountAdministrationService>();
builder.Services.AddScoped<E2EDatabaseLifecycleService>();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy
            .WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (E2EDatabaseCommand.IsRequested(args))
{
    if (!E2EDatabaseCommand.TryParse(args, out var command, out var error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    await using var scope = app.Services.CreateAsyncScope();
    var lifecycle = scope.ServiceProvider.GetRequiredService<E2EDatabaseLifecycleService>();
    var result = command!.Action == "prepare"
        ? await lifecycle.PrepareAsync(command.DatabaseName)
        : await lifecycle.CleanupAsync(command.DatabaseName);

    if (result.Succeeded && command.Action == "prepare")
    {
        await scope.ServiceProvider.GetRequiredService<AppRoleInitializer>().InitializeAsync();
        var password = command.ReadPassword();
        var bootstrap = string.IsNullOrWhiteSpace(password)
            ? AdminBootstrapResult.Failure("The E2E Admin password environment variable is missing.")
            : await scope.ServiceProvider.GetRequiredService<AdminBootstrapService>()
                .BootstrapAsync(command.AdminEmail!, password, false);
        result = bootstrap.Succeeded
            ? result
            : E2EDatabaseResult.Failure(bootstrap.Message);
    }

    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    Environment.ExitCode = result.Succeeded ? 0 : 1;
    return;
}

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppRoleInitializer>().InitializeAsync();
}

if (AdminBootstrapCommand.IsRequested(args))
{
    if (!AdminBootstrapCommand.TryParse(args, out var command, out var error))
    {
        Console.Error.WriteLine(error);
        Environment.ExitCode = 1;
        return;
    }

    await using var scope = app.Services.CreateAsyncScope();
    var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
    var existingUserManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var existingUser = await existingUserManager.FindByEmailAsync(command!.Email.Trim());
    var password = existingUser is null ? command.ReadPassword() : null;
    var result = await service.BootstrapAsync(command.Email, password, command.ConfirmExistingUser);
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    Environment.ExitCode = result.Succeeded ? 0 : 1;
    return;
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapHealthEndpoints();
app.MapTodoEndpoints();

app.Run();

public partial class Program;
