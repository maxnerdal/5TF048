using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    public class LiveTradingBackgroundService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LiveTradingBackgroundService> _logger;
        private Timer? _timer;

        public LiveTradingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<LiveTradingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Live Trading Background Service is starting");
            _timer = new Timer(ExecuteActiveBots, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            return Task.CompletedTask;
        }

        private async void ExecuteActiveBots(object? state)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tradingBotService = scope.ServiceProvider.GetRequiredService<ITradingBotService>();

            try
            {
                var currentTime = DateTime.UtcNow;
                
                // Get active bots where NextExecutionTime has been reached
                var activeBots = await context.UserBots
                    .Include(ub => ub.TradingBot)
                    .Where(ub => ub.Status == "Active" 
                        && ub.NextExecutionTime.HasValue 
                        && ub.NextExecutionTime.Value <= currentTime)
                    .ToListAsync();

                _logger.LogInformation($"Checking for bots to execute. Found {activeBots.Count} bot(s) ready to run");

                foreach (var bot in activeBots)
                {
                    try
                    {
                        _logger.LogInformation($"Executing bot {bot.UserBotId} ({bot.Name}). Next execution was: {bot.NextExecutionTime}");
                        
                        var success = await tradingBotService.ExecuteBotStrategyAsync(bot.UserBotId);
                        
                        if (success)
                        {
                            _logger.LogInformation($"Bot {bot.UserBotId} ({bot.Name}) executed successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error executing bot {bot.UserBotId} ({bot.Name})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in live trading background service");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Live Trading Background Service is stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
