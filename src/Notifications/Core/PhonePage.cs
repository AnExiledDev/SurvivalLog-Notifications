using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Notifications.Core;

/// <summary>The game's words and pictures the phone list needs on top of a notice's own text.</summary>
public interface IViewWords : IGameWords
{
    /// <summary>An item's picture, as a path from the game's WebUI folder, or null.</summary>
    string? ItemIcon(int configId);

    string? BuffName(int buffId);
}

/// <summary>
/// The history as the phone app draws it, in JSON. The writer escapes everything outside ASCII and every character
/// that could end a script, so the JSON can be pasted into JavaScript as it is.
/// </summary>
public static class PhoneView
{
    /// <summary>Rows from a popup the game can draw again, rows the game showed as a short message, and other mods' toasts.</summary>
    public const string Popup = "popup";
    public const string Message = "msg";
    public const string Mod = "mod";

    private static readonly (string Key, string English, string Chinese)[] Words =
    {
        ("title", "Notifications", "通知"),
        ("new", "new", "条新"),
        ("all", "All", "全部"),
        ("unread", "New", "未读"),
        ("popup", "Popups", "弹窗"),
        ("msg", "Messages", "提示"),
        ("mod", "Mods", "模组"),
        ("today", "TODAY", "今天"),
        ("open", "Open popup", "打开弹窗"),
        ("openPlain", "Open", "打开"),
        ("markRead", "Mark all read", "全部已读"),
        ("clear", "Clear list", "清空列表"),
        ("clearAsk", "Clear the list?", "清空列表？"),
        ("yes", "Clear", "清空"),
        ("no", "Keep", "保留"),
        ("kept", "Kept with this save: {0} of {1}", "随存档保存：{0} / {1}"),
        ("more", "+{0} more", "另有 {0} 种"),
        ("hintPopup", "Opens the popup as the game showed it", "按游戏原样打开弹窗"),
        ("hintNone", "No popup for this notice", "此通知没有弹窗"),
        ("emptyTitle", "No notices yet", "暂无通知"),
        ("emptyHint", "Popups, game messages and mod notices from this save will show here.", "本存档的弹窗、游戏提示和模组通知都会显示在这里。"),
        ("emptyFilter", "Nothing in this filter", "此分类暂无通知"),
        ("seeAll", "See all", "查看全部"),
        ("recent", "NOTIFICATIONS", "通知"),
        ("game", "Game", "游戏"),
    };

    /// <summary>Without the game's tables: item names fall back to their numbers and nothing has a picture.</summary>
    public static string Json(History? history, bool isChinese) => Json(history, new NoWords(isChinese), null);

    /// <param name="today">The in-game day now, which the list calls today; null when not known.</param>
    public static string Json(History? history, IViewWords words, int? today)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("label", words.IsChinese ? "通知" : "Notifications");
            json.WriteNumber("unread", history?.UnreadCount ?? 0);
            json.WriteNumber("mark", history?.LatestMark ?? 0);
            json.WriteNumber("kept", history?.Entries.Count ?? 0);
            json.WriteNumber("cap", History.Cap);

            if (today is { } day) json.WriteNumber("today", day);

            json.WriteStartObject("text");

            foreach (var (key, english, chinese) in Words) json.WriteString(key, words.IsChinese ? chinese : english);

            json.WriteEndObject();
            json.WriteStartArray("rows");

            if (history != null)
            {
                foreach (var entry in history.Entries) WriteRow(json, entry, history.IsUnread(entry), words);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>A row's filter: a popup the game can draw again, a short game message, or another mod's toast.</summary>
    public static string Category(HistoryEntry entry) => entry.Reopen != null ? Popup : IsGame(entry) ? Message : Mod;

    /// <summary>The small picture for a row that has no item of its own.</summary>
    public static string Glyph(HistoryEntry entry) => entry.Reopen switch
    {
        Reopen.Wish => "star",
        Reopen.Items => entry.Content.Kind == NoteKind.Info ? "drone" : "box",
        Reopen.Wave => "shield",
        Reopen.Skill => "up",
        _ when !IsGame(entry) => "plug",
        _ => entry.Content.Kind switch
        {
            NoteKind.Success => "unlock",
            NoteKind.Warning or NoteKind.Error => "warn",
            _ => "msg",
        },
    };

    private static bool IsGame(HistoryEntry entry) => entry.Content.Source == NoteTexts.Source(false) || entry.Content.Source == NoteTexts.Source(true);

    private static void WriteRow(Utf8JsonWriter json, HistoryEntry entry, bool isUnread, IViewWords words)
    {
        json.WriteStartObject();
        json.WriteNumber("seq", entry.Seq);
        json.WriteString("source", entry.Content.Source);
        json.WriteBoolean("game", IsGame(entry));
        json.WriteString("title", entry.Content.Title);
        json.WriteString("body", Body(entry, words) ?? "");
        json.WriteNumber("count", entry.Count);

        if (entry.Stamp is { } stamp)
        {
            json.WriteNumber("day", stamp.Day);
            json.WriteNumber("hour", stamp.Hour);
        }

        json.WriteString("kind", HistoryJson.KindName(entry.Content.Kind));
        json.WriteString("cat", Category(entry));
        json.WriteString("glyph", Glyph(entry));

        if (Icon(entry, words) is { } icon) json.WriteString("icon", icon);

        json.WriteStartArray("items");

        if (entry.Reopen is Reopen.Items items)
        {
            foreach (var stack in items.Stacks)
            {
                json.WriteStartObject();
                json.WriteString("name", NoteTexts.ItemName(words, stack.ConfigId));
                json.WriteNumber("count", stack.Count);

                if (words.ItemIcon(stack.ConfigId) is { } itemIcon) json.WriteString("icon", itemIcon);

                json.WriteEndObject();
            }
        }

        json.WriteEndArray();
        json.WriteStartArray("stats");

        var stats = entry.Reopen switch
        {
            Reopen.Wish wish => NoteTexts.WishStats(words, wish),
            Reopen.Wave wave => NoteTexts.WaveStats(words, wave),
            _ => Array.Empty<NoteStat>(),
        };

        foreach (var stat in stats)
        {
            json.WriteStartObject();
            json.WriteString("label", stat.Label);
            json.WriteString("value", stat.Value);
            json.WriteBoolean("up", stat.IsUp);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteBoolean("popup", entry.Reopen != null);
        json.WriteBoolean("click", entry.IsClickable);
        json.WriteBoolean("unread", isUnread);
        json.WriteEndObject();
    }

    /// <summary>
    /// The text under the title. A popup's numbers and items are drawn as stats and chips, so its text keeps only what
    /// they do not show.
    /// </summary>
    private static string? Body(HistoryEntry entry, IViewWords words) => entry.Reopen switch
    {
        Reopen.Wish wish => Joined(words, NoteTexts.WishLine(words, wish), words.BuffName(wish.BuffId)),
        Reopen.Items items => NoteTexts.Plain(items.Desc),
        Reopen.Wave wave => NoteTexts.WaveSupplies(words, wave),
        _ => entry.Content.Body,
    };

    private static string? Icon(HistoryEntry entry, IViewWords words) => entry.Reopen switch
    {
        Reopen.Wish { ItemConfigId: > 0 } wish => words.ItemIcon(wish.ItemConfigId) ?? entry.Icon,
        Reopen.Items { Stacks.Count: > 0 } items => words.ItemIcon(items.Stacks[0].ConfigId) ?? entry.Icon,
        _ => entry.Icon,
    };

    private static string? Joined(IGameWords words, params string?[] parts)
    {
        var kept = parts.Select(NoteTexts.Plain).Where(part => !string.IsNullOrEmpty(part)).ToList();

        return kept.Count == 0 ? null : string.Join(words.IsChinese ? "，" : ", ", kept);
    }

    private sealed class NoWords : IViewWords
    {
        public NoWords(bool isChinese) => IsChinese = isChinese;

        public bool IsChinese { get; }

        public string? Constant(string id) => null;

        public string? ItemName(int configId) => null;

        public string? ItemIcon(int configId) => null;

        public string? BuffName(int buffId) => null;
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

    /// <summary>The player marked everything up to this mark read.</summary>
    public sealed record Seen(long Mark) : PageMessage;

    /// <summary>The player opened this row in the list.</summary>
    public sealed record Read(long Seq) : PageMessage;

    /// <summary>The player cleared the list, as it stood at this mark.</summary>
    public sealed record Clear(long Mark) : PageMessage;

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
            "read" when long.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var row) => new Read(row),
            "clear" when long.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var upTo) => new Clear(upTo),
            "noscript" => new NoScript(),
            "diag" => new Diag(argument),
            _ => new Unknown(rest),
        };
    }
}
