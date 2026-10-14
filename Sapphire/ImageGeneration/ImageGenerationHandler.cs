using System.Collections.Concurrent;
using Sapphire.Environments;

namespace Sapphire.ImageGeneration;

public class ImageGenerationHandler
{
    private static readonly Queue<Task<string>> _taskQueue = new();
    private readonly object _lock = new();

    private readonly ConcurrentDictionary<Task<string>, TaskCompletionSource<string>> _pending = new();

    private readonly List<Task> _runningTasks = new();

    private readonly SemaphoreSlim _semaphore = new(
        SpEnvironmentHandler.Environment.ImageGeneration.MaxParallelGenerationTaskCount,
        SpEnvironmentHandler.Environment.ImageGeneration.MaxParallelGenerationTaskCount);

    private Task _runningTask;

    private ImageGenerationHandler()
    {
        _runningTask = Task.Run(DispatchLoopAsync);
    }

    public static ImageGenerationHandler Instance { get; } = new();

    public Task<string> AddTask(Task<string> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var tcs = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _pending[task] = tcs;

        lock (_lock)
        {
            _taskQueue.Enqueue(task);
        }

        return tcs.Task;
    }

    private async Task DispatchLoopAsync()
    {
        while (true)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            Task<string>? next = null;
            lock (_lock)
            {
                if (_taskQueue.Count > 0)
                    next = _taskQueue.Dequeue();
            }

            if (next == null)
            {
                _semaphore.Release();
                await Task.Delay(50).ConfigureAwait(false);
                continue;
            }

            var wrapper = RunAndReleaseAsync(next);

            lock (_lock)
            {
                _runningTasks.Add(wrapper);
            }

            _ = wrapper.ContinueWith(t =>
            {
                lock (_lock)
                {
                    _runningTasks.Remove(t);
                }
            }, TaskScheduler.Default);
        }
    }

    private async Task RunAndReleaseAsync(Task<string> task)
    {
        try
        {
            var result = await task.ConfigureAwait(false);

            if (_pending.TryRemove(task, out var tcs))
                tcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            if (_pending.TryRemove(task, out var tcs))
                tcs.TrySetException(ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}