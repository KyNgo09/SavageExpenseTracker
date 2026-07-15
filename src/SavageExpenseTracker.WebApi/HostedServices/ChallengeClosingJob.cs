using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.HostedServices
{
    public class ChallengeClosingJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ChallengeClosingJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var challengeService = scope.ServiceProvider.GetRequiredService<IChallengeService>();
                    await challengeService.ProcessExpiredChallengesAsync();
                }
                
                // Job quét 1 giờ 1 lần
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
