using WEB_Demo.Api.Endpoints;
using WEB_Demo.Api.Middleware;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var corsOptions = builder.Configuration
    .GetSection(ApiCorsOptions.SectionName)
    .Get<ApiCorsOptions>() ?? new ApiCorsOptions();

builder.Services.Configure<ApiCorsOptions>(
    builder.Configuration.GetSection(ApiCorsOptions.SectionName));

builder.Services.AddSingleton<ITodoService, InMemoryTodoService>();
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

app.UseMiddleware<ApiExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ClientApp");

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapHealthEndpoints();
app.MapTodoEndpoints();

app.Run();

public partial class Program;
