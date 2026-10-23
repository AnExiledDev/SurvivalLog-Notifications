using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Notifications.Core;

/// <summary>What reading a history file came to.</summary>
public abstract record HistoryRead
{
    private HistoryRead()
    {
    }

    public sealed record Loaded(History History, int SkippedEntries) : HistoryRead;

    /// <summary>The file is not a history this version can read; it is kept aside rather than overwritten.</summary>
    public sealed record Corrupt(string Reason) : HistoryRead;
}

/// <summary>
/// A save's history as a JSON file. Toolkit toast ids are not written: they mean nothing after a restart. The writer's
/// default escaping keeps the file ASCII, so no text in it depends on the file's encoding.
/// </summary>
public static class HistoryJson
{
    public const int Version = 1;

    public static string Write(History history)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteNumber("version", Version);
            json.WriteNumber("next", history.Next);
            json.WriteNumber("seen", history.SeenMark);
            json.WriteStartArray("entries");

            foreach (var entry in history.Entries) WriteEntry(json, entry);

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>Never throws. An entry that cannot be read is skipped and counted; the rest still load.</summary>
    public static HistoryRead Read(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return new HistoryRead.Corrupt("not a JSON object");

            if (Long(root, "version") != Version) return new HistoryRead.Corrupt("unknown version");

            if (!root.TryGetProperty("entries", out var list) || list.ValueKind != JsonValueKind.Array) return new HistoryRead.Corrupt("no entries list");

            var entries = new List<HistoryEntry>();
            var skipped = 0;

            foreach (var item in list.EnumerateArray())
            {
                if (ReadEntry(item) is { } entry) entries.Add(entry);
                else skipped++;
            }

            return new HistoryRead.Loaded(new History(entries, Long(root, "next") ?? 1, Long(root, "seen") ?? 0), skipped);
        }
        catch (JsonException ex)
        {
            return new HistoryRead.Corrupt("not JSON: " + ex.Message);
        }
    }

    private static void WriteEntry(Utf8JsonWriter json, HistoryEntry entry)
    {
        json.WriteStartObject();
        json.WriteNumber("seq", entry.Seq);
        json.WriteNumber("mark", entry.Mark);

        if (entry.ReadMark > 0) json.WriteNumber("read", entry.ReadMark);

        json.WriteString("source", entry.Content.Source);
        json.WriteString("title", entry.Content.Title);

        if (entry.Content.Body != null) json.WriteString("body", entry.Content.Body);

        json.WriteString("kind", KindName(entry.Content.Kind));
        WriteOptional(json, "icon", entry.Icon);
        json.WriteNumber("count", entry.Count);
        json.WriteString("first", entry.FirstUtc.ToString("O", CultureInfo.InvariantCulture));
        json.WriteString("last", entry.LastUtc.ToString("O", CultureInfo.InvariantCulture));

        if (entry.Stamp is { } stamp)
        {
            json.WriteNumber("day", stamp.Day);
            json.WriteNumber("hour", stamp.Hour);
        }

        if (entry.Reopen != null)
        {
            json.WritePropertyName("reopen");
            WriteReopen(json, entry.Reopen);
        }

        json.WriteEndObject();
    }

    private static void WriteReopen(Utf8JsonWriter json, Reopen reopen)
    {
        json.WriteStartObject();

        switch (reopen)
        {
            case Reopen.Wish wish:
                json.WriteString("type", "wish");
                json.WriteNumber("item", wish.ItemConfigId);
                json.WriteNumber("faith", wish.FaithValue);
                json.WriteNumber("survival", wish.SurvivalValue);
                json.WriteNumber("buff", wish.BuffId);
                json.WriteNumber("pool", wish.WishPoolId);
                break;
            case Reopen.Items items:
                json.WriteString("type", "items");
                json.WriteStartArray("stacks");

                foreach (var stack in items.Stacks)
                {
                    json.WriteStartArray();
                    json.WriteNumberValue(stack.ConfigId);
                    json.WriteNumberValue(stack.Count);
                    json.WriteEndArray();
                }

                json.WriteEndArray();

                if (items.Kinds != null)
                {
                    json.WriteStartArray("kinds");

                    foreach (var kind in items.Kinds) json.WriteNumberValue(kind);

                    json.WriteEndArray();
                }

                json.WriteNumber("bumper", items.BumperCount);
                json.WriteNumber("super", items.SuperCount);
                WriteOptional(json, "presentation", items.Presentation);
                WriteOptional(json, "desc", items.Desc);
                break;
            case Reopen.Wave wave:
                json.WriteString("type", "wave");
                json.WriteNumber("wave", wave.WaveIndex);
                json.WriteNumber("kills", wave.KillCount);
                json.WriteNumber("device", wave.DeviceDamage);
                json.WriteNumber("structure", wave.StructureDamage);
                WriteOptional(json, "reward", wave.RewardText);
                break;
            case Reopen.Skill skill:
                json.WriteString("type", "skill");
                json.WriteStartObject("fields");

                foreach (var field in skill.Fields.OrderBy(field => field.Key, StringComparer.Ordinal)) json.WriteString(field.Key, field.Value);

                json.WriteEndObject();
                break;
        }

        json.WriteEndObject();
    }

    private static void WriteOptional(Utf8JsonWriter json, string name, string? value)
    {
        if (value != null) json.WriteString(name, value);
    }

    private static HistoryEntry? ReadEntry(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;

        var seq = Long(item, "seq");
        var source = Text(item, "source");
        var title = Text(item, "title");

        if (seq is not > 0 || source == null || title == null) return null;

        var mark = Long(item, "mark") ?? seq.Value;
        var first = Time(item, "first") ?? DateTime.MinValue;
        var last = Time(item, "last") ?? first;
        var day = Long(item, "day");
        var hour = Long(item, "hour");
        var stamp = day != null && hour != null ? new GameStamp((int)day.Value, (int)hour.Value) : (GameStamp?)null;
        var reopen = item.TryGetProperty("reopen", out var payload) ? ReadReopen(payload) : null;
        var content = new Note(source, title, Text(item, "body"), KindOf(Text(item, "kind")));

        return new HistoryEntry(seq.Value, mark, content, (int)Math.Max(1, Long(item, "count") ?? 1), first, last, stamp, reopen)
        {
            ReadMark = Math.Max(0, Long(item, "read") ?? 0),
            Icon = Text(item, "icon"),
        };
    }

    /// <summary>A payload this version cannot rebuild leaves the entry readable but not clickable.</summary>
    private static Reopen? ReadReopen(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;

        switch (Text(payload, "type"))
        {
            case "wish":
                return new Reopen.Wish(Int(payload, "item"), Int(payload, "faith"), Int(payload, "survival"), Int(payload, "buff"), Int(payload, "pool"));
            case "items":
                if (!payload.TryGetProperty("stacks", out var stacks) || stacks.ValueKind != JsonValueKind.Array) return null;

                var read = stacks.EnumerateArray().Select(StackOf).ToList();

                if (read.Any(stack => stack == null)) return null;

                return new Reopen.Items(read.Select(stack => stack!.Value).ToList(), Ints(payload, "kinds"), Int(payload, "bumper"), Int(payload, "super"),
                    Text(payload, "presentation"), Text(payload, "desc"));
            case "wave":
                return new Reopen.Wave(Int(payload, "wave"), Int(payload, "kills"), Int(payload, "device"), Int(payload, "structure"), Text(payload, "reward"));
            case "skill":
                if (!payload.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Object) return null;

                var map = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var field in fields.EnumerateObject())
                {
                    if (field.Value.ValueKind == JsonValueKind.String) map[field.Name] = field.Value.GetString() ?? "";
                }

                return new Reopen.Skill(map);
            default:
                return null;
        }
    }

    private static ItemStack? StackOf(JsonElement pair)
    {
        if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2) return null;

        if (!pair[0].TryGetInt32(out var id) || !pair[1].TryGetInt32(out var count)) return null;

        return new ItemStack(id, count);
    }

    private static List<int>? Ints(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var list) || list.ValueKind != JsonValueKind.Array) return null;

        return list.EnumerateArray().Select(value => value.TryGetInt32(out var number) ? number : 0).ToList();
    }

    public static string KindName(NoteKind kind) => kind switch
    {
        NoteKind.Success => "success",
        NoteKind.Warning => "warning",
        NoteKind.Error => "error",
        _ => "info",
    };

    private static NoteKind KindOf(string? name) => name switch
    {
        "success" => NoteKind.Success,
        "warning" => NoteKind.Warning,
        "error" => NoteKind.Error,
        _ => NoteKind.Info,
    };

    private static long? Long(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static int Int(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : 0;

    private static string? Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static DateTime? Time(JsonElement item, string name) =>
        Text(item, name) is { } text && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ? time : null;
}
