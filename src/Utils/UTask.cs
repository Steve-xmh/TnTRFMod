using System.Collections;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;
using Exception = Il2CppSystem.Exception;
using YieldAwaitable = Cysharp.Threading.Tasks.YieldAwaitable;

#if BEPINEX
using Cysharp.Threading.Tasks;
#endif

#if MELONLOADER
using Il2CppCysharp.Threading.Tasks;
#endif

namespace TnTRFMod.Utils;

public readonly struct UTask
{
    private readonly UniTask uniTask;

    public UTask(UniTask uniTask)
    {
        this.uniTask = uniTask;
    }

    public static implicit operator UTask(UniTask uniTask)
    {
        return new UTask(uniTask);
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(uniTask.GetAwaiter());
    }

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly UniTask.Awaiter uAwaiter;

        public Awaiter(UniTask.Awaiter uAwaiter)
        {
            this.uAwaiter = uAwaiter;
        }

        public bool IsCompleted => uAwaiter.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            uAwaiter.OnCompleted(continuation);
        }

        public void GetResult()
        {
            uAwaiter.GetResult();
        }
    }
}

public readonly struct UTask<T>
{
    private readonly UniTask<T> uniTask;

    public UTask(UniTask<T> uniTask)
    {
        this.uniTask = uniTask;
    }

    public static implicit operator UTask<T>(UniTask<T> uniTask)
    {
        return new UTask<T>(uniTask);
    }

    public Awaiter<T> GetAwaiter()
    {
        return new Awaiter<T>(uniTask.GetAwaiter());
    }

    public readonly struct Awaiter<UT> : INotifyCompletion
    {
        private readonly UniTask<UT>.Awaiter uAwaiter;

        public Awaiter(UniTask<UT>.Awaiter awaiter)
        {
            uAwaiter = awaiter;
        }

        public bool IsCompleted => uAwaiter.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            uAwaiter.OnCompleted(continuation);
        }

        public UT GetResult()
        {
            return uAwaiter.GetResult();
        }
    }
}

public static class UTaskExt
{
    public static UTask<T> ToTask<T>(this UniTask<T> uniTask)
    {
        return new UTask<T>(uniTask);
    }

    public static UTask ToTask(this UniTask uniTask)
    {
        return new UTask(uniTask);
    }

    public static UTask ToTask(this YieldAwaitable awaitable)
    {
        return new UTask(awaitable.ToUniTask());
    }

    public static UniTask ToUniTask(this Task uniTask)
    {
        var pred = DelegateSupport.ConvertDelegate<Il2CppSystem.Func<bool>>(() => uniTask.IsCompleted);
        return UniTask.WaitUntil(pred, PlayerLoopTiming.Update, new CancellationToken(false));
    }

    public static IEnumerator Await<T>(this UniTask<T> uniTask, Action<T> onResult = null,
        Action<System.Exception> onException = null)
    {
        var result = default(T);
        Exception ex = null;
        var co = uniTask.ToCoroutine(
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<T>>((T r) => { result = r; }
            ),
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Exception>>((Exception exception) => { ex = exception; }
            )
        );

        yield return co;
        if (ex != null)
        {
            if (onException == null)
                Logger.Error($"Failed to execute UniTask: {ex}");
            else
                onException.Invoke(new System.Exception(ex.Message));
        }
        else
        {
            onResult?.Invoke(result);
        }
    }
}