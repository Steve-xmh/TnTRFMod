namespace TnTRFMod.Utils;

public sealed class AsyncLock
{
    private readonly object _lock = new();
    private bool _locked;

    public async Task<Guard> LockAsync()
    {
        var success = false;
        while (!success)
        {
            lock (_lock)
            {
                if (!_locked)
                {
                    _locked = true;
                    success = true;
                }
            }

            // Should we use some notifier-like method to avoid polling?
            await Task.Yield();
        }

        return new Guard(this);
    }

    public sealed class Guard(AsyncLock thisLock) : IDisposable
    {
        public void Dispose()
        {
            lock (thisLock._lock)
            {
                thisLock._locked = false;
            }
        }
    }
}