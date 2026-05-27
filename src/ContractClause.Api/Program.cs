using ContractClause.Api.BackgroundServices;
using ContractClause.Api.Middleware;
using ContractClause.Api.Services;
using ContractClause.Application;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Infrastructure;
using ContractClause.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<ApiUserContext>();
    builder.Services.AddScoped<IUserContext>(sp => sp.GetRequiredService<ApiUserContext>());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>();
    builder.Services.AddHostedService<TemplateSyncBackgroundService>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    app.MapHealthChecks("/healthz");
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseMiddleware<ApiKeyAuthMiddleware>();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
