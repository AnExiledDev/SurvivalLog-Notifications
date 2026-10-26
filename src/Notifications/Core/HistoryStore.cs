using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Notifications.Core;

/// <summary>A history as it was at one moment, safe to write from another thread: its entries never change.</summary>
public sealed record HistorySnapshot(long Next, long SeenMark, IReadOnlyList<HistoryEntry> Entries);

/// <summary>What the store found under a save's name.</summary>
public abstract record StoredText
{
    private StoredText()
    {
    }

    public sealed record Missing : StoredText;

    public sealed record Found(string Text) : StoredText;

    /// <summary>The file is there but could not be read; it must not be overwritten.</summary>
    public sealed record Unreadable(string Reason) : StoredText;
}

/// <summary>Where each save's history file lives. Implementations never throw.</summary>
public interface IHistoryStore
{
    StoredText Read(string fileName);

    /// <summary>
    /// Replaces the file whole with the history as it was at the call, so a crash mid-write leaves the old one. The
    /// write may finish after the call returns. Null when it was taken, else the reason; a failure found later comes
    /// from <see cref="TakeFailure"/>.
    /// </summary>
    string? Write(string fileName, HistorySnapshot snapshot);

    /// <summary>The reason a write failed after <see cref="Write"/> returned, handed out once; null when there is none.</summary>
    string? TakeFailure();

    /// <summary>Moves a file this version cannot read out of the way, keeping it. Null on success, else the reason.</summary>
    string? SetAside(string fileName);
}

/// <summary>The history file's name for a save.</summary>
public static class SaveFileName
{
    private const int LongestName = 80;

    /// <summary>
    /// The save's own file name with anything a file name cannot hold replaced, so a name can never reach outside the
    /// history folder. A name that had to change gets a hash of the original, so two saves never share a history.
    /// </summary>
    public static string For(string saveKey)
    {
        var name = new StringBuilder(saveKey.Length);

        foreach (var character in saveKey.Take(LongestName))
        {
            name.Append(IsPlain(character) ? character : '_');
        }

        var isUnchanged = name.Length > 0 && saveKey.Length <= LongestName && saveKey.All(IsPlain);

        if (!isUnchanged) name.Append('-').Append(Hash(saveKey).ToString("x8"));

        return name + ".json";
    }

    private static bool IsPlain(char character) => character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_';

    /// <summary>FNV-1a: the same on every run, unlike string.GetHashCode.</summary>
    private static uint Hash(string text)
    {
        var hash = 2166136261u;

        foreach (var character in text)
        {
            hash = unchecked((hash ^ (uint)character) * 16777619u);
        }

        return hash;
    }
}
