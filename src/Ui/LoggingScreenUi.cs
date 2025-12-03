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

    private static void Update()
    {
        if (_textUi == null) Init();

        _textBuilder.Clear();
        for (var i = 0; i < _handles.Count; i++)
        {
            var handle = _handles[i];
            _textBuilder.AppendLine(handle.Text);
        }

        _textUi!.Text = _textBuilder.ToString();
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
            _handles.Add(this);
            Update();
        }

        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                Update();
            }
        }

        public void Dispose()
        {
            _handles.Remove(this);
            Update();
        }
    }

    public class ThreadSafeLogHandle : LogHandleBase, IDisposable
    {
        private string _text = "";

        internal ThreadSafeLogHandle(string text)
        {
            _text = text;
            _handles.Add(this);
            UTask.RunOnIl2CppBlocking(Update);
        }

        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                UTask.RunOnIl2CppBlocking(Update);
            }
        }

        public void Dispose()
        {
            _handles.Remove(this);
            UTask.RunOnIl2CppBlocking(Update);
        }
    }

    public class AsyncLogHandle : LogHandleBase, IAsyncDisposable
    {
        private string _text = "";

        internal AsyncLogHandle()
        {
            _handles.Add(this);
        }

        public override string Text
        {
            get => _text;
            set => _ = SetTextAsync(value);
        }

        public async ValueTask DisposeAsync()
        {
            _handles.Remove(this);
            await SafeUpdate();
        }

        public async Task SetTextAsync(string text)
        {
            _text = text;
            await SafeUpdate();
        }
    }
}