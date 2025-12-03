using System.Collections;
using System.Runtime.CompilerServices;
#if BEPINEX
using Il2CppInterop.Runtime;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;
using Exception = Il2CppSystem.Exception;
using Object = Il2CppSystem.Object;
using YieldAwaitable = Cysharp.Threading.Tasks.YieldAwaitable;
using Cysharp.Threading.Tasks;

#elif MELONLOADER
using Il2CppCysharp.Threading.Tasks;
using Il2CppInterop.Runtime;
using YieldAwaitable = Il2CppCysharp.Threading.Tasks.YieldAwaitable;
using Object = Il2CppSystem.Object;
using Exception = Il2CppSystem.Exception;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;

#endif

namespace TnTRFMod.Utils;

public readonly struct UTask(UniTask uniTask)
{
    public static implicit operator UTask(UniTask uniTask)
    {
        return new UTask(uniTask);
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(uniTask.GetAwaiter());
    }

    public static Task RunOnIl2Cpp(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        TnTrfMod.Instance.RunOnMainThread.Enqueue(() =>
        {
            try
            {
                action.Invoke();
                taskCompletionSource.SetResult();
            }
            catch (System.Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });
        return taskCompletionSource.Task;
    }

    public static void RunOnIl2CppBlocking(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var curThreadId = Environment.CurrentManagedThreadId;
        if (curThreadId == 1)
        {
            action.Invoke();
            return;
        }

        TnTrfMod.Instance.RunOnMainThread.Enqueue(() =>
        {
            try
            {
                action.Invoke();
                taskCompletionSource.SetResult();
            }
            catch (System.Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });
        taskCompletionSource.Task.Wait();
    }

    public static async Task RunOnIl2CppThreadPool(Action action, TaskCompletionSource? taskCompletionSource = null)
    {
        taskCompletionSource ??= new TaskCompletionSource();
        await RunOnIl2Cpp(() =>
        {
            UniTask.RunOnThreadPool(DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(() =>
            {
                try
                {
                    action.Invoke();
                    taskCompletionSource.SetResult();
                }
                catch (System.Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }), true, new CancellationToken(false)).Forget();
        });
        await taskCompletionSource.Task;
    }

    public static void RunOnIl2CppThreadPoolBlocking(Action action)
    {
        RunOnIl2CppThreadPool(action).Wait();
    }

    public static Task<T> RunOnIl2Cpp<T>(Func<T> action)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        TnTrfMod.Instance.RunOnMainThread.Enqueue(() =>
        {
            try
            {
                taskCompletionSource.SetResult(action.Invoke());
            }
            catch (System.Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });
        return taskCompletionSource.Task;
    }

    public static async Task<T> RunOnIl2Cpp<T>(Func<UniTask<T>> action)
    {
        var taskCompletionSource = new TaskCompletionSource<UniTask<T>>();
        TnTrfMod.Instance.RunOnMainThread.Enqueue(() =>
        {
            try
            {
                taskCompletionSource.SetResult(action.Invoke());
            }
            catch (System.Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });
        return await await taskCompletionSource.Task;
    }

    public static async Task<T> RunOnIl2CppThreadPool<T>(Func<T> action,
        TaskCompletionSource<T>? taskCompletionSource = null)
    {
        taskCompletionSource ??= new TaskCompletionSource<T>();
        await RunOnIl2Cpp(() =>
        {
            Il2CppSystem.Threading.Tasks.Task.Run(DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(() =>
            {
                try
                {
                    taskCompletionSource.SetResult(action.Invoke());
                }
                catch (System.Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }));
        });
        return await taskCompletionSource.Task;
    }

    public static T RunOnIl2CppThreadPoolBlocking<T>(Func<T> action)
    {
        var task = RunOnIl2CppThreadPool(action);
        task.Wait();
        return task.Result;
    }

    public static async Task<T> RunOnIl2CppThreadPool<T>(Func<UniTask<T>> action)
    {
        var taskCompletionSource = new TaskCompletionSource<UniTask<T>>();
        await RunOnIl2Cpp(() =>
        {
            UniTask.RunOnThreadPool(DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(() =>
            {
                try
                {
                    taskCompletionSource.SetResult(action.Invoke());
                }
                catch (System.Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }), true, CancellationToken.None).Forget();
        });
        return await await taskCompletionSource.Task;
    }

    public static T RunOnIl2CppThreadPoolBlocking<T>(Func<UniTask<T>> action)
    {
        var task = RunOnIl2CppThreadPool(action);
        task.Wait();
        return task.Result;
    }

    public readonly struct Awaiter(UniTask.Awaiter uAwaiter) : INotifyCompletion
    {
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

public readonly struct UTask<T>(UniTask<T> uniTask)
{
    public static implicit operator UTask<T>(UniTask<T> uniTask)
    {
        return new UTask<T>(uniTask);
    }

    public Awaiter<T> GetAwaiter()
    {
        return new Awaiter<T>(uniTask.GetAwaiter());
    }

    public readonly struct Awaiter<UT>(UniTask<UT>.Awaiter awaiter) : INotifyCompletion
    {
        public bool IsCompleted => awaiter.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            awaiter.OnCompleted(continuation);
        }

        public UT GetResult()
        {
            return awaiter.GetResult();
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

    public static UniTask ToUniTask(this Task thisTask)
    {
        var source = new UniTaskCompletionSource();
        thisTask.ContinueWith(async _ =>
        {
            await UTask.RunOnIl2Cpp(() =>
            {
                if (thisTask.IsFaulted)
                    source.TrySetException(new Exception(thisTask.Exception?.Message ?? "Task was faulted."));
                else if (thisTask.IsCanceled)
                    source.TrySetCanceled();
                else
                    source.TrySetResult();
            });
        });
        return source.Task;
    }

    public static UniTask<T> ToUniTask<T>(this Task<T> thisTask) where T : Object
    {
        var source = new UniTaskCompletionSource<T>();
        thisTask.ContinueWith(async _ =>
        {
            await UTask.RunOnIl2Cpp(() =>
            {
                if (thisTask.IsFaulted)
                    source.TrySetException(new Exception(thisTask.Exception?.Message ?? "Task was faulted."));
                else if (thisTask.IsCanceled)
                    source.TrySetCanceled();
                else
                    source.TrySetResult(thisTask.Result);
            });
        });
        return source.Task;
    }

    public static IEnumerator Await<T>(this UniTask<T> uniTask, Action<T>? onResult = null,
        Action<System.Exception>? onException = null)
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