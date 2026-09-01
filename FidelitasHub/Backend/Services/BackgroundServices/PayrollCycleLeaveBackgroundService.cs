using FidelitasHub.Services.Leave;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FidelitasHub.Services.BackgroundServices
{
    public class PayrollCycleLeaveBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PayrollCycleLeaveBackgroundService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var payrollCycleLeaveService =
                        scope.ServiceProvider
                            .GetRequiredService<PayrollCycleLeaveService>();

                    await payrollCycleLeaveService.SynchronizeAsync();
                }
                catch (Exception)
                {
                    // Background synchronization will retry on
                    // the next cycle. Add logging later if required.
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(15),
                    stoppingToken);
            }
        }
    }
}
