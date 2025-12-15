#if BEPINEX
using System.Collections;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;
using Logger = TnTRFMod.Utils.Logger;

// ReSharper disable ClassNeverInstantiated.Global

namespace TnTRFMod.Loader;

[BepInPlugin(TnTrfMod.MOD_GUID, TnTrfMod.MOD_NAME, TnTrfMod.MOD_VERSION)]
public class BepInExPlugin : BasePlugin
{
    private BepInExCoroutineRunner _runner;
    public static BepInExPlugin Instance { get; private set; }

    public override void Load()
    {
        Instance = this;
        Logger._inner = Log;
        _runner = AddComponent<BepInExCoroutineRunner>();
        TnTrfMod.Instance = new TnTrfMod
        {
            _runner = _runner
        };
        TnTrfMod.Instance.Load(new Harmony(TnTrfMod.MOD_GUID));
    }

    internal class BepInExCoroutineRunner : MonoBehaviour, TnTrfMod.CoroutineRunner
    {
#pragma warning disable CA1822
        public void Update()
#pragma warning restore CA1822
        {
            TnTrfMod.Instance.OnUpdate();
        }

        [HideFromIl2Cpp]
        public void RunCoroutine(Il2CppIEnumerator routine)
        {
            StartCoroutine(routine);
        }

        [HideFromIl2Cpp]
        public void RunCoroutine(IEnumerable routine)
        {
            this.StartCoroutine(ExecCoroutineWithIEnumerable(routine));
        }

        [HideFromIl2Cpp]
        public void RunCoroutine(IEnumerator routine)
        {
            this.StartCoroutine(routine);
        }

        [HideFromIl2Cpp]
        private static IEnumerator ExecCoroutineWithIEnumerable(IEnumerable routine)
        {
            yield return routine;
        }
    }
}

#endif