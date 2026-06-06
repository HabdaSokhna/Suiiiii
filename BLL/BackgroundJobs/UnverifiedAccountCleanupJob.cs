using Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database.Domain;

namespace BLL.BackgroundJobs
{
    public class UnverifiedAccountCleanupJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UnverifiedAccountCleanupJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupUnverifiedAccounts();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        // ✅ الكود الجديد هنا
        private async Task CleanupUnverifiedAccounts()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Ai_Reports_Context>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            var unverifiedUsers = await userManager.Users
                .Where(u => !u.EmailConfirmed && u.CreatedAt <= fiveMinutesAgo)
                .ToListAsync();

            foreach (var user in unverifiedUsers)
            {
                try
                {
                    var citizen = await context.TbCitizen
                        .Include(c => c.LstPhone)
                        .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

                    if (citizen != null)
                    {
                        context.TbCitizen_Phone.RemoveRange(citizen.LstPhone);
                        context.TbCitizen.Remove(citizen);
                        await context.SaveChangesAsync();
                    }

                    await userManager.DeleteAsync(user);
                    Console.WriteLine($"🗑️ تم مسح حساب غير مفعّل: {user.Email}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ خطأ: {ex.Message}");
                }
            }
        }
    }
}