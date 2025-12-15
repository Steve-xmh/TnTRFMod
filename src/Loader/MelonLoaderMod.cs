#if MELONLOADER
using System.Collections;
using System.Reflection.Emit;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;
using Object = UnityEngine.Object;

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
        Object.DontDestroyOnLoad(modGo);
        TnTrfMod.Instance = new TnTrfMod
        {
            _runner = runner
        };
        TnTrfMod.Instance.Load(HarmonyInstance);
    }

    public override void OnUpdate()
    {
        TnTrfMod.Instance.OnUpdate();
    }

    public static Il2CppIEnumerator ConvertToIl2CppIEnumerator(IEnumerator routine)
    {
        return new Il2CppManagedEnumerator(routine).Cast<Il2CppIEnumerator>();
    }

    public class Il2CppManagedEnumerator : Object
    {
        private static readonly Dictionary<Type, Func<object, Object>> boxers = new();

        private readonly IEnumerator enumerator;

        static Il2CppManagedEnumerator()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Il2CppManagedEnumerator>(new RegisterTypeOptions
            {
                Interfaces = new[] { typeof(Il2CppIEnumerator) }
            });
        }

        public Il2CppManagedEnumerator(IntPtr ptr, IEnumerator enumerator) : base(ptr)
        {
            this.enumerator = enumerator;
        }

        public Il2CppManagedEnumerator(IEnumerator enumerator)
            : base(ClassInjector.DerivedConstructorPointer<Il2CppManagedEnumerator>())
        {
            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            ClassInjector.DerivedConstructorBody(this);
        }

        public Object Current => (enumerator.Current switch
        {
            Il2CppIEnumerator i => i.Cast<Object>(),
            IEnumerator e => new Il2CppManagedEnumerator(e),
            Object oo => oo,
            { } obj => ManagedToIl2CppObject(obj),
            null => null
        })!;

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public void Reset()
        {
            enumerator.Reset();
        }

        private static Func<object, Object> GetValueBoxer(Type t)
        {
            if (boxers.TryGetValue(t, out var conv))
                return conv;

            var dm = new DynamicMethod($"Il2CppUnbox_{t.FullDescription()}", typeof(Object),
                new[] { typeof(object) });
            var il = dm.GetILGenerator();
            var loc = il.DeclareLocal(t);
            var classField = typeof(Il2CppClassPointerStore<>).MakeGenericType(t)
                .GetField(nameof(Il2CppClassPointerStore<int>
                    .NativeClassPtr))!;
            il.Emit(OpCodes.Ldsfld, classField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Unbox_Any, t);
            il.Emit(OpCodes.Stloc, loc);
            il.Emit(OpCodes.Ldloca, loc);
            il.Emit(OpCodes.Call,
                typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box))!);
            il.Emit(OpCodes.Newobj, typeof(Object).GetConstructor([typeof(IntPtr)])!);
            il.Emit(OpCodes.Ret);

            var converter = (dm.CreateDelegate(typeof(Func<object, Object>)) as Func<object, Object>)!;
            boxers[t] = converter;
            return converter;
        }

        private static Object ManagedToIl2CppObject(object obj)
        {
            var t = obj.GetType();
            if (obj is string s)
                return new Object(IL2CPP.ManagedStringToIl2Cpp(s));
            if (t.IsPrimitive)
                return GetValueBoxer(t)(obj);
            throw new NotSupportedException($"Type {t} cannot be converted directly to an Il2Cpp object");
        }
    }


    [RegisterTypeInIl2Cpp]
    private class MelonCoroutineRunner : MonoBehaviour, TnTrfMod.CoroutineRunner
    {
        public MelonCoroutineRunner(IntPtr ptr) : base(ptr)
        {
        }

#pragma warning disable CA1822
        public void Update()
#pragma warning restore CA1822
        {
            TnTrfMod.Instance.OnUpdate();
        }

        [HideFromIl2Cpp]
        public void RunCoroutine(IEnumerator routine)
        {
            MelonCoroutines.Start(routine);
        }

        [HideFromIl2Cpp]
        public void RunCoroutine(Il2CppIEnumerator routine)
        {
            MelonCoroutines.Start(ExecCoroutineWithIEnumerable(routine));
        }

        [HideFromIl2Cpp]
        public void RunCoroutine(IEnumerable routine)
        {
            MelonCoroutines.Start(ExecCoroutineWithIEnumerable(routine));
        }

        [HideFromIl2Cpp]
        private static IEnumerator ExecCoroutineWithIEnumerable(Il2CppIEnumerator routine)
        {
            while (routine.MoveNext()) yield return routine.Current;
        }

        [HideFromIl2Cpp]
        private static IEnumerator ExecCoroutineWithIEnumerable(IEnumerable routine)
        {
            yield return routine;
        }
    }
}
#endif