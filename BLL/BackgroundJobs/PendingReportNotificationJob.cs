using BLL.Service;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BLL.BackgroundJobs
{
    public class PendingReportNotificationJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PendingReportNotificationJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckPendingReports();
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task CheckPendingReports()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Ai_Reports_Context>();
            var notifService = scope.ServiceProvider.GetRequiredService<IAuthorityNotificationService>();

            var oneHourAgo = DateTime.UtcNow.AddHours(-24);

            var pendingHandles = await context.TbHandle
                .Include(h => h.Report)
                .Where(h => h.Status == "Pending" && h.Report.CreatedAt <= oneHourAgo)
                .ToListAsync();

            foreach (var handle in pendingHandles)
            {
                await notifService.SendAsync(handle.Authority_ID, "PendingReport", handle.Report_ID);
            }
        }
    }
}