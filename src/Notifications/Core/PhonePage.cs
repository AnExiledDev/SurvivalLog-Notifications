using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Notifications.Core;

/// <summary>
/// The history as the phone app draws it, in JSON. The writer escapes everything outside ASCII and every character
/// that could end a script, so the JSON can be pasted into JavaScript as it is.
/// </summary>
public static class PhoneView
{
    public static string Json(History? history, bool isChinese)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("label", isChinese ? "通知" : "Notifications");
            json.WriteString("empty", isChinese ? "暂无通知" : "No notifications yet");
            json.WriteNumber("unread", history?.UnreadCount ?? 0);
            json.WriteNumber("mark", history?.LatestMark ?? 0);
            json.WriteStartArray("rows");

            foreach (var entry in history?.Entries ?? Array.Empty<HistoryEntry>()) WriteRow(json, entry, history?.SeenMark ?? 0);

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteRow(Utf8JsonWriter json, HistoryEntry entry, long seenMark)
    {
        json.WriteStartObject();
        json.WriteNumber("seq", entry.Seq);
        json.WriteString("source", entry.Content.Source);
        json.WriteString("title", entry.Content.Title);
        json.WriteString("body", entry.Content.Body ?? "");
        json.WriteNumber("count", entry.Count);

        if (entry.Stamp is { } stamp)
        {
            json.WriteNumber("day", stamp.Day);
            json.WriteNumber("hour", stamp.Hour);
        }

        json.WriteBoolean("click", entry.IsClickable);
        json.WriteBoolean("unread", entry.Mark > seenMark);
        json.WriteEndObject();
    }
}

/// <summary>The JavaScript the plugin runs in the game's web view, which runs it in the root page.</summary>
public static class PageCalls
{
    public const string Global = "window.__slnotify";

    public const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + PageMessage.Prefix + "noscript')";

    /// <summary>Silent while the script is there; asks for it when a reload lost it.</summary>
    public const string Probe = Global + "||(" + NoScriptReply + ")";

    public static string Set(string viewJson) => $"{Global}?{Global}.set({viewJson}):({NoScriptReply})";

    public static string Install(string script, string viewJson) => script + "\n;" + Set(viewJson);
}

/// <summary>What the plugin should send the page this frame.</summary>
public enum PhoneStep
{
    Wait,
    Install,
    Update,
    Probe,
}

/// <summary>
/// When to talk to the page: the script goes in first and again whenever the page says it is gone, the history goes
/// over when it changed (at most every <see cref="UpdateSeconds"/>), and every <see cref="ProbeSeconds"/> the page is
/// asked whether the script is still there.
/// </summary>
public sealed class PhoneSync
{
    public const double ProbeSeconds = 2;
    public const double UpdateSeconds = 0.5;

    private bool _needsInstall = true;
    private long _sentRevision = long.MinValue;
    private double _nextProbeAt;
    private double _nextSendAt;

    public PhoneStep Next(double now, long revision)
    {
        if (_needsInstall) return now < _nextSendAt ? PhoneStep.Wait : Sent(now, revision, PhoneStep.Install);

        if (revision != _sentRevision && now >= _nextSendAt) return Sent(now, revision, PhoneStep.Update);

        if (now < _nextProbeAt) return PhoneStep.Wait;

        _nextProbeAt = now + ProbeSeconds;

        return PhoneStep.Probe;
    }

    /// <summary>The page said the script is missing: put it back with the next step.</summary>
    public void ScriptMissing() => _needsInstall = true;

    /// <summary>A step could not be sent, as there was no web view: try again after a probe's wait.</summary>
    public void NotSent(double now)
    {
        _needsInstall = true;
        _nextSendAt = now + ProbeSeconds;
    }

    private PhoneStep Sent(double now, long revision, PhoneStep step)
    {
        _needsInstall = false;
        _sentRevision = revision;
        _nextSendAt = now + UpdateSeconds;
        _nextProbeAt = now + ProbeSeconds;

        return step;
    }
}

/// <summary>A message from the page script.</summary>
public abstract record PageMessage
{
    public const string Prefix = "slmod|notifications|";

    private PageMessage()
    {
    }

    /// <summary>The player clicked a history row.</summary>
    public sealed record Open(long Seq) : PageMessage;

    /// <summary>The player opened the list and saw everything up to this mark.</summary>
    public sealed record Seen(long Mark) : PageMessage;

    public sealed record NoScript : PageMessage;

    /// <summary>Something the script wants in the log.</summary>
    public sealed record Diag(string Text) : PageMessage;

    public sealed record Unknown(string Text) : PageMessage;

    public static bool IsOurs(string? message) => message != null && message.StartsWith(Prefix, StringComparison.Ordinal);

    public static PageMessage Parse(string message)
    {
        var rest = IsOurs(message) ? message.Substring(Prefix.Length) : message;
        var bar = rest.IndexOf('|');
        var verb = bar < 0 ? rest : rest.Substring(0, bar);
        var argument = bar < 0 ? "" : rest.Substring(bar + 1);

        return verb switch
        {
            "open" when long.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var seq) => new Open(seq),
            "seen" when long.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var mark) => new Seen(mark),
            "noscript" => new NoScript(),
            "diag" => new Diag(argument),
            _ => new Unknown(rest),
        };
    }
}
