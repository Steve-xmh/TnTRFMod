namespace TnTRFMod.Utils;

public sealed class AsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Guard> LockAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        return new Guard(this);
    }

    public sealed class Guard(AsyncLock thisLock) : IDisposable
    {
        public void Dispose()
        {
            thisLock._semaphore.Release();
        }
    }
}