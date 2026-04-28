using System.Text;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;

namespace TnTRFMod.Ui;

public static class LoggingScreenUi
{
    private static TextUi? _textUi;
    private static readonly StringBuilder _textBuilder = new(1024);
    private static readonly List<LogHandleBase> _handles = new(16);
    private static readonly object _handlesLock = new();
    private static bool _isDirty;

    private static void Init()
    {
        _textUi = new TextUi
        {
            Text = "",
            Position = new Vector2(64f, 128f),
            FontSize = 24
        };
        _textUi.MoveToNoDestroyCanvas();
    }

    private static void MarkDirty()
    {
        _isDirty = true;
    }

    private static void Update()
    {
        if (!_isDirty) return;
        if (_textUi == null) Init();

        _textBuilder.Clear();
        lock (_handlesLock)
        {
            for (var i = 0; i < _handles.Count; i++)
            {
                var handle = _handles[i];
                _textBuilder.AppendLine(handle.Text);
            }
        }

        _textUi!.Text = _textBuilder.ToString();
        _isDirty = false;
    }

    private static async Task SafeUpdate()
    {
        await UTask.RunOnIl2Cpp(Update);
    }

    public static LogHandle New(string text = "")
    {
        return new LogHandle(text);
    }

    public static ThreadSafeLogHandle NewThreadSafe(string text = "")
    {
        return new ThreadSafeLogHandle(text);
    }

    public static AsyncLogHandle NewAsync()
    {
        return new AsyncLogHandle();
    }

    public abstract class LogHandleBase
    {
        public abstract string Text { get; set; }
    }

    public class LogHandle : LogHandleBase, IDisposable
    {
        private string _text = "";

        internal LogHandle(string text)
        {
            _text = text;
            lock (_handlesLock)
            {
                _handles.Add(this);
            }

            MarkDirty();
            Update();
        }

        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                MarkDirty();
                Update();
            }
        }

        public void Dispose()
        {
            lock (_handlesLock)
            {
                _handles.Remove(this);
            }

            MarkDirty();
            Update();
        }
    }

    public class ThreadSafeLogHandle : LogHandleBase, IDisposable
    {
        private string _text = "";

        internal ThreadSafeLogHandle(string text)
        {
            _text = text;
            lock (_handlesLock)
            {
                _handles.Add(this);
            }

            MarkDirty();
            UTask.RunOnIl2CppBlocking(Update);
        }

        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                MarkDirty();
                UTask.RunOnIl2CppBlocking(Update);
            }
        }

        public void Dispose()
        {
            lock (_handlesLock)
            {
                _handles.Remove(this);
            }

            MarkDirty();
            UTask.RunOnIl2CppBlocking(Update);
        }
    }

    public class AsyncLogHandle : LogHandleBase, IAsyncDisposable
    {
        private string _text = "";

        internal AsyncLogHandle()
        {
            lock (_handlesLock)
            {
                _handles.Add(this);
            }

            MarkDirty();
        }

        public override string Text
        {
            get => _text;
            set => _ = SetTextAsync(value);
        }

        public async ValueTask DisposeAsync()
        {
            lock (_handlesLock)
            {
                _handles.Remove(this);
            }

            MarkDirty();
            await SafeUpdate();
        }

        public async Task SetTextAsync(string text)
        {
            _text = text;
            MarkDirty();
            await SafeUpdate();
        }
    }
}