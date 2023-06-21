using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ViGo.Utilities.Extensions;

namespace ViGo.Utilities.BackgroundTasks
{
    public class QueuedHostedServices : BackgroundService
    {
        private readonly ILogger<QueuedHostedServices> _logger;
        public IBackgroundTaskQueue TaskQueue { get; set; }

        public QueuedHostedServices(IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedServices> logger)
        {
            TaskQueue = taskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);
                try
                {
                    _logger.LogInformation($"{workItem} is dequeued...");
                    await workItem(cancellationToken);
                } catch (Exception ex)
                {
                    _logger.LogError($"Error occured executing {workItem}.{Environment.NewLine}" +
                        $"Details: {ex.GeneratorErrorMessage()}");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}
