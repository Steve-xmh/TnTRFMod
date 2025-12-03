#if MELONLOADER
using System.Collections;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;

// ReSharper disable ClassNeverInstantiated.Global

namespace TnTRFMod.Loader;

public class MelonLoaderMod : MelonMod
{
    public static MelonLoaderMod Instance { get; private set; }
    
    public override void OnInitializeMelon()
    {
        Instance = this;
        ClassInjector.RegisterTypeInIl2Cpp<MelonCoroutineRunner>();
        Logger._inner = LoggerInstance;
        var modGo = new GameObject("TnTrfMod_MelonCoroutineRunner");
        var runner = modGo.AddComponent<MelonCoroutineRunner>();
        UnityEngine.Object.DontDestroyOnLoad(modGo);
        TnTrfMod.Instance = new TnTrfMod
        {
            _runner = runner,
        };
        TnTrfMod.Instance.Load(HarmonyInstance);
    }

    public override void OnUpdate()
    {
        TnTrfMod.Instance.OnUpdate();
    }

    [RegisterTypeInIl2Cpp]
    private class MelonCoroutineRunner : MonoBehaviour, TnTrfMod.CoroutineRunner
    {
        public MelonCoroutineRunner(IntPtr ptr) : base(ptr) {}
        
        public void RunCoroutine(IEnumerator routine)
        {
            MelonCoroutines.Start(routine);
        }

        public new void RunCoroutine(Il2CppIEnumerator routine)
        {
            MelonCoroutines.Start(ExecCoroutineWithIEnumerable(routine));
        }

        public void RunCoroutine(IEnumerable routine)
        {
            MelonCoroutines.Start(ExecCoroutineWithIEnumerable(routine));
        }
        
        private static IEnumerator ExecCoroutineWithIEnumerable(Il2CppIEnumerator routine)
        {
            while (routine.MoveNext()) yield return routine.Current;
        }
        
        private static IEnumerator ExecCoroutineWithIEnumerable(IEnumerable routine)
        {
            yield return routine;
        }
        
#pragma warning disable CA1822
        public void Update()
#pragma warning restore CA1822
        {
            TnTrfMod.Instance.OnUpdate();
        }
    }
}
#endif