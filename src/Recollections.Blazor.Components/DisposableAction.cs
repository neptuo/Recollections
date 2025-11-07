using System;
using System.Threading.Tasks;

namespace Neptuo.Recollections;

public class DisposableAction(Action action) : IDisposable
{
    private bool isDisposed;

    public void Dispose()
    {
        if (!isDisposed)
        {
            action();
            isDisposed = true;
        }
    }
}

public class AsyncDisposableAction(Func<Task> action) : IAsyncDisposable
{
    private bool isDisposed;

    public async ValueTask DisposeAsync()
    {
        if (!isDisposed)
        {
            await action();
            isDisposed = true;
        }
    }
}