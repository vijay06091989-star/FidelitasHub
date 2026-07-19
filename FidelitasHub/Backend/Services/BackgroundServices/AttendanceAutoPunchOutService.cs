using Microsoft.Extensions.DependencyInjection;
using FidelitasHub.Services.Attendance;
using Microsoft.Extensions.Hosting;

namespace FidelitasHub.Services.BackgroundServices
{
    public class AttendanceAutoPunchOutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AttendanceAutoPunchOutService(
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

                    var attendanceService =
                        scope.ServiceProvider
                            .GetRequiredService<IAttendanceProcessingService>();

                    await attendanceService.ProcessAutoPunchOutAsync();
                }
                catch (Exception ex)
                {
                    // TODO:
                    // Log ex.Message into AuditLog or a Log table later.
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}