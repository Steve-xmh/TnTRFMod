using System.Collections;
using Il2CppInterop.Runtime;
using Exception = Il2CppSystem.Exception;

#if BEPINEX
using Cysharp.Threading.Tasks;
#endif

#if MELONLOADER
using Il2CppCysharp.Threading.Tasks;
#endif

namespace TnTRFMod.Utils;

public static class UTask
{
    // public static Task<T> ToTask<T>(this UniTask<T> uniTask)
    // {
    //     var tcs = new TaskCompletionSource<T>();
    //
    //     var co = uniTask.ToCoroutine(
    //         DelegateSupport.ConvertDelegate<Il2CppSystem.Action<T>>(
    //             (T result) => { tcs.SetResult(result); }
    //         ),
    //         DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Exception>>(
    //             (Exception result) => { tcs.SetException(new System.Exception(result.Message)); }
    //         )
    //     );
    //
    //     TnTrfMod.Instance.StartCoroutine(co);
    //
    //     return tcs.Task;
    // }

    public static IEnumerator Await<T>(this UniTask<T> uniTask, Action<T> onResult = null,
        Action<System.Exception> onException = null)
    {
        var result = default(T);
        Exception ex = null;
        var co = uniTask.ToCoroutine(
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<T>>(
                (T r) => { result = r; }
            ),
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Exception>>(
                (Exception exception) => { ex = exception; }
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