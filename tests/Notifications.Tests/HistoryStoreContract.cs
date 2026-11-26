using Notifications.Adapters;
using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

/// <summary>What the ledger counts on from a history store, checked on the fake its tests use and on the real folder.</summary>
public abstract class HistoryStoreContract
{
    protected abstract IHistoryStore NoFilesYet();

    [Fact]
    public void ANameNeverWrittenIsMissing() => Assert.IsType<StoredText.Missing>(NoFilesYet().Read("save1.json"));

    [Fact]
    public void AWrittenHistoryReadsBack()
    {
        var store = NoFilesYet();

        Assert.Null(store.Write("save1.json", HistoryOf("kept").Snapshot()));

        Assert.Equal(new[] { "kept" }, TitlesIn(store, "save1.json"));
        Assert.Null(store.TakeFailure());
    }

    [Fact]
    public void ANewerWriteReplacesTheOlder()
    {
        var store = NoFilesYet();

        store.Write("save1.json", HistoryOf("first").Snapshot());
        store.Read("save1.json");
        store.Write("save1.json", HistoryOf("second").Snapshot());

        Assert.Equal(new[] { "second" }, TitlesIn(store, "save1.json"));
    }

    [Fact]
    public void WritingOneSaveLeavesAnotherMissing()
    {
        var store = NoFilesYet();

        store.Write("save1.json", HistoryOf("kept").Snapshot());

        Assert.IsType<StoredText.Missing>(store.Read("save2.json"));
    }

    [Fact]
    public void AFileSetAsideIsMissingAfterwards()
    {
        var store = NoFilesYet();

        store.Write("save1.json", HistoryOf("kept").Snapshot());

        Assert.Null(store.SetAside("save1.json"));
        Assert.IsType<StoredText.Missing>(store.Read("save1.json"));
    }

    private static History HistoryOf(string title)
    {
        var history = new History();

        history.Record(Make.Toast(1, title), null, null);

        return history;
    }

    private static string[] TitlesIn(IHistoryStore store, string fileName)
    {
        var found = Assert.IsType<StoredText.Found>(store.Read(fileName));

        return Make.Titles(Assert.IsType<HistoryRead.Loaded>(HistoryJson.Read(found.Text)).History);
    }
}

public sealed class FakeStoreTests : HistoryStoreContract
{
    protected override IHistoryStore NoFilesYet() => new FakeStore();
}

/// <summary>The history folder on disk, in a new temp folder that is only made at the first write.</summary>
public sealed class HistoryFilesTests : HistoryStoreContract, IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "NotificationsHistory-" + Guid.NewGuid().ToString("N"));
    private readonly HistoryFiles _files;

    public HistoryFilesTests() => _files = new HistoryFiles(_folder);

    public void Dispose()
    {
        _files.Settle(TimeSpan.FromSeconds(5));

        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    protected override IHistoryStore NoFilesYet() => _files;

    /// <summary>If it were taken for a missing file, a new history could be saved over it.</summary>
    [Fact]
    public void SomethingThereThatCannotBeReadIsUnreadableNeverMissing()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "save1.json"));

        Assert.IsType<StoredText.Unreadable>(_files.Read("save1.json"));
    }
}
