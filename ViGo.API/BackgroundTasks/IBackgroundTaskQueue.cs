namespace ViGo.API.BackgroundTasks
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackGroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
        ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
    }
}
