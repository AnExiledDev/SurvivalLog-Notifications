using System;
using System.Collections.Generic;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// The plugin's contact with the phone app: it hands the page the history, and acts on the player's clicks there. All
/// members run on Unity's main thread.
/// </summary>
internal sealed class PhoneApp
{
    private const string ScriptResource = "Notifications.notifications.js";

    private readonly PhoneSync _sync = new();
    private readonly PhoneList _list = new();
    private readonly Ledger _ledger;
    private readonly NoticeDesk _desk;
    private readonly Action<string> _log;
    private readonly HashSet<string> _reported = new();
    private string? _script;

    public PhoneApp(Ledger ledger, NoticeDesk desk, Action<string> log)
    {
        _ledger = ledger;
        _desk = desk;
        _log = log;
    }

    public void Tick(double now)
    {
        while (PageMessages.TryTake(out var message)) Handle(message);

        var step = _sync.Next(now, _ledger.Revision);

        switch (step)
        {
            case PhoneStep.Install:
                // A page the script goes into has none of the list yet.
                _list.Forget();
                Run(PageCalls.Install(Script(), View()), now);

                break;
            case PhoneStep.Update:
                Run(PageCalls.Set(View()), now);

                break;
            case PhoneStep.Probe:
                Run(PageCalls.Probe, now);

                break;
        }
    }

    private void Handle(PageMessage message)
    {
        switch (message)
        {
            case PageMessage.Open open:
                _desk.OpenRow(open.Seq);

                break;
            case PageMessage.Seen seen:
                _ledger.MarkSeen(seen.Mark);

                break;
            case PageMessage.Read read:
                _ledger.MarkRead(read.Seq);

                break;
            case PageMessage.Clear clear:
                _ledger.Clear(clear.Mark);

                break;
            case PageMessage.NoScript:
                _sync.ScriptMissing();

                break;
            case PageMessage.Resync:
                _list.Forget();
                _sync.Resend();

                break;
            case PageMessage.Diag diag:
                LogOnce("diag:" + Before(diag.Text, ':'), "phone app: " + diag.Text);

                break;
            case PageMessage.Unknown unknown:
                LogOnce("unknown:" + Before(unknown.Text, '|'), "phone app sent a message this version does not know: " + unknown.Text);

                break;
        }
    }

    private string View() => _list.Json(_ledger.Current, new GameWords(WebIcons.Shared(_log)), GameState.Stamp()?.Day);

    private void Run(string javaScript, double now)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null)
        {
            _sync.NotSent(now);

            return;
        }

        try
        {
            webView.ExecuteJavaScript(javaScript, null);
        }
        catch (Exception ex)
        {
            _sync.NotSent(now);
            LogOnce("run", "could not reach the phone page: " + ex.Message);
        }
    }

    /// <summary>
    /// One line per kind of problem, keyed on its name rather than its text: an error text that carries changing values
    /// would otherwise log, and be kept, once per value.
    /// </summary>
    private void LogOnce(string kind, string line)
    {
        if (_reported.Add(kind)) _log(line);
    }

    /// <summary>The name a message starts with: the page's problem before its ':', or a message's verb before its '|'.</summary>
    private static string Before(string text, char mark)
    {
        var at = text.IndexOf(mark);

        return at < 0 ? text : text.Substring(0, at);
    }

    /// <summary>Read on first need: most launches send it once.</summary>
    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(PhoneApp).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
