using Microsoft.EntityFrameworkCore;
using FullPost.Context;
using FullPost.Interfaces.Services;
namespace FullPost;
public class FullPostBackgroundService : BackgroundService
{
    IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FullPostBackgroundService> _logger;
    public FullPostBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<FullPostBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    protected async override Task ExecuteAsync(CancellationToken token)
    {
        using var vscope = _serviceScopeFactory.CreateScope();
        var context = vscope.ServiceProvider.GetRequiredService<FullPostContext>();
        await context.Database.MigrateAsync();
        var task1 = Task.Run(() => ResetMonthlyPostCountAsync(token), token);
        var task2 = Task.Run(() => ResetToBasic(token), token);
        await Task.WhenAll(task1, task2);
        await Task.CompletedTask;
    }
    private async Task ResetMonthlyPostCountAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

            await service.ResetMonthlyPostCountAsync();

            _logger.LogInformation("Reset Monthly Post Count executed.");

            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }
    private async Task ResetToBasic(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

            await service.ResetToBasic();

            _logger.LogInformation("Reset To Basic executed.");

            await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
        }
    }
}
