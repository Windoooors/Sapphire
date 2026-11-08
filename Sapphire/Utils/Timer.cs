using Serilog;

namespace Sapphire.Utils;

public class Timer : IDisposable
{
    private readonly object _lock = new();
    private readonly System.Threading.Timer _timer;
    private readonly TimeOnly _triggerTimeUtc;
    private bool _disposed;
    private DateOnly _lastTriggeredDate;

    public Timer(TimeOnly triggerTimeUtc)
        : this(triggerTimeUtc, TimeSpan.FromSeconds(10))
    {
    }

    public Timer(TimeOnly triggerTimeUtc, TimeSpan checkInterval)
    {
        _triggerTimeUtc = triggerTimeUtc;

        _timer = new System.Threading.Timer(Check, null, checkInterval, checkInterval);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        _timer.Dispose();
    }

    public event Action? DayChanged;

    private void Check(object? state)
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            if (today <= _lastTriggeredDate)
                return;

            var triggerToday = now.Date.Add(_triggerTimeUtc.ToTimeSpan());
            if (now < triggerToday)
                return;

            _lastTriggeredDate = today;
        }

        try
        {
            DayChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in timer check");
        }
    }
}