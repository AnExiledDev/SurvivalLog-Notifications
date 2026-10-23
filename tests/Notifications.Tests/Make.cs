using Notifications.Core;

namespace Notifications.Tests;

/// <summary>Small builders the tests share, so each test shows only what it is about.</summary>
internal static class Make
{
    public static readonly DateTime T0 = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    public static Note Notice(string title, string? body = null) => new("Game", title, body, NoteKind.Info);

    public static ToastNote Toast(long id, string title, int secondsLater = 0, bool isMerge = false, bool hasClickAction = false) =>
        new(id, Notice(title), T0.AddSeconds(secondsLater), hasClickAction, isMerge);

    public static Reopen.Wish Wish(int itemConfigId = 5) => new(itemConfigId, 3, 0, 0, 2);

    public static Reopen.Items Items(string? desc, params (int Id, int Count)[] stacks) =>
        new(stacks.Select(stack => new ItemStack(stack.Id, stack.Count)).ToList(), null, 0, 0, null, desc);

    public static IReadOnlyList<ItemStack> Cargo(params (int Id, int Count)[] stacks) => stacks.Select(stack => new ItemStack(stack.Id, stack.Count)).ToList();

    public static string[] Titles(History history) => history.Entries.Select(entry => entry.Content.Title).ToArray();
}

/// <summary>The history folder in memory. Like the real one it never throws; failures are set by the test.</summary>
internal sealed class FakeStore : IHistoryStore
{
    public Dictionary<string, StoredText> Files { get; } = new();

    public List<string> SetAsideNames { get; } = new();

    public string? WriteFailure { get; set; }

    public string? SetAsideFailure { get; set; }

    public StoredText Read(string fileName) => Files.TryGetValue(fileName, out var text) ? text : new StoredText.Missing();

    public string? Write(string fileName, string text)
    {
        if (WriteFailure != null) return WriteFailure;

        Files[fileName] = new StoredText.Found(text);

        return null;
    }

    public string? SetAside(string fileName)
    {
        if (SetAsideFailure != null) return SetAsideFailure;

        Files.Remove(fileName);
        SetAsideNames.Add(fileName);

        return null;
    }

    /// <summary>The titles in a saved history file, newest first, or null when there is no file.</summary>
    public string[]? SavedTitles(string fileName) =>
        Files.TryGetValue(fileName, out var stored) && stored is StoredText.Found found && HistoryJson.Read(found.Text) is HistoryRead.Loaded loaded
            ? Make.Titles(loaded.History)
            : null;
}

/// <summary>The game's words, from tables the test fills.</summary>
internal sealed class FakeWords : IViewWords
{
    public FakeWords(bool isChinese = false) => IsChinese = isChinese;

    public bool IsChinese { get; }

    public Dictionary<string, string> Constants { get; } = new();

    public Dictionary<int, string> Items { get; } = new();

    public string? Constant(string id) => Constants.TryGetValue(id, out var text) ? text : null;

    public string? ItemName(int configId) => Items.TryGetValue(configId, out var name) ? name : null;

    public Dictionary<int, string> Icons { get; } = new();

    public Dictionary<int, string> Buffs { get; } = new();

    public string? ItemIcon(int configId) => Icons.TryGetValue(configId, out var icon) ? icon : null;

    public string? BuffName(int buffId) => Buffs.TryGetValue(buffId, out var name) ? name : null;
}
