using ContractClause.Application.Options;
using ContractClause.Application.Templates.Sync;
using Microsoft.Extensions.Options;

namespace ContractClause.Api.BackgroundServices;

public class TemplateSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<FatianshiTemplateSyncOptions> options,
    ILogger<TemplateSyncBackgroundService> logger) : BackgroundService
{
    private readonly FatianshiTemplateSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("法天使模板同步已禁用 (FatianshiTemplateSync:Enabled=false)");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, _options.IntervalHours));
        logger.LogInformation("法天使模板同步已启动，间隔 {Hours} 小时", _options.IntervalHours);

        using var timer = new PeriodicTimer(interval);
        do
        {
            await RunSyncAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sync = scope.ServiceProvider.GetRequiredService<ITemplateSyncService>();
            await sync.RunAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "法天使模板同步任务异常");
        }
    }
}
