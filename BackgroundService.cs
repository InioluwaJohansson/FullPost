using Microsoft.EntityFrameworkCore;
using FullPost.Context;
namespace FullPost;
public class FullPostBackgroundService : BackgroundService
{
    IServiceScopeFactory _serviceScopeFactory;
    public FullPostBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    protected async override Task ExecuteAsync(CancellationToken token)
    {
        using var vscope = _serviceScopeFactory.CreateScope();
        var context = vscope.ServiceProvider.GetRequiredService<FullPostContext>();
        await context.Database.MigrateAsync();
        await Task.CompletedTask;
    }
}
