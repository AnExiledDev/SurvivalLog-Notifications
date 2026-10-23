using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class LedgerTests
{
    private readonly FakeStore _store = new();
    private readonly List<string> _log = new();
    private readonly Ledger _ledger;

    public LedgerTests() => _ledger = new Ledger(_store, _log.Add);

    [Fact]
    public void OutOfAGameNothingIsKept()
    {
        _ledger.Follow(false, "save1", 0);

        Assert.Null(_ledger.Record(Make.Toast(1, "title screen"), null, null));
        Assert.Null(_ledger.Current);
    }

    [Fact]
    public void LoadingASaveBringsBackItsHistory()
    {
        _store.Files["save1.json"] = new StoredText.Found(Saved("from last time"));

        _ledger.Follow(true, "save1", 0);

        Assert.Equal(new[] { "from last time" }, Make.Titles(Current()));
    }

    [Fact]
    public void NoticesBeforeTheSaveIsNamedJoinItsHistoryOnTop()
    {
        _store.Files["save1.json"] = new StoredText.Found(Saved("old"));

        _ledger.Follow(true, null, 0);
        _ledger.Record(Make.Toast(1, "early"), null, null);
        _ledger.Follow(true, "save1", 1);

        Assert.Equal(new[] { "early", "old" }, Make.Titles(Current()));
    }

    [Fact]
    public void LeavingToTheTitleScreenSavesTheHistoryAndClearsIt()
    {
        _ledger.Follow(true, "save1", 0);
        _ledger.Record(Make.Toast(1, "kept"), null, null);
        _ledger.Follow(false, null, 0.1);

        Assert.Null(_ledger.Current);
        Assert.Equal(new[] { "kept" }, _store.SavedTitles("save1.json"));
    }

    [Fact]
    public void ChangingSaveSavesTheFirstAndLoadsTheSecond()
    {
        _store.Files["save2.json"] = new StoredText.Found(Saved("second's"));

        _ledger.Follow(true, "save1", 0);
        _ledger.Record(Make.Toast(1, "first's"), null, null);
        _ledger.Follow(true, "save2", 0.1);

        Assert.Equal(new[] { "first's" }, _store.SavedTitles("save1.json"));
        Assert.Equal(new[] { "second's" }, Make.Titles(Current()));
    }

    [Fact]
    public void ChangesAreWrittenAtMostEveryFlushIntervalUnlessForced()
    {
        _ledger.Follow(true, "save1", 0);
        _ledger.Record(Make.Toast(1, "a"), null, null);
        _ledger.Flush(10);
        _ledger.Record(Make.Toast(2, "b"), null, null);
        _ledger.Flush(10 + Ledger.FlushSeconds / 2);

        Assert.Equal(new[] { "a" }, _store.SavedTitles("save1.json"));

        _ledger.Flush(10 + Ledger.FlushSeconds);

        Assert.Equal(new[] { "b", "a" }, _store.SavedTitles("save1.json"));

        _ledger.Record(Make.Toast(3, "c"), null, null);
        _ledger.Flush(10 + Ledger.FlushSeconds, isForced: true);

        Assert.Equal(new[] { "c", "b", "a" }, _store.SavedTitles("save1.json"));
    }

    [Fact]
    public void ACorruptFileIsKeptAsideAndANewHistoryStarted()
    {
        _store.Files["save1.json"] = new StoredText.Found("garbage");

        _ledger.Follow(true, "save1", 0);
        _ledger.Record(Make.Toast(1, "new"), null, null);
        _ledger.Flush(5, isForced: true);

        Assert.Equal(new[] { "save1.json" }, _store.SetAsideNames);
        Assert.Equal(new[] { "new" }, _store.SavedTitles("save1.json"));
        Assert.Single(_log);
    }

    [Fact]
    public void AFileThatCannotBeReadOrMovedAsideIsNeverOverwritten()
    {
        _store.Files["save1.json"] = new StoredText.Unreadable("locked");
        _store.Files["save2.json"] = new StoredText.Found("garbage");
        _store.SetAsideFailure = "denied";

        foreach (var save in new[] { "save1", "save2" })
        {
            _ledger.Follow(true, save, 0);
            _ledger.Record(Make.Toast(1, "new"), null, null);
            _ledger.Flush(5, isForced: true);
        }

        Assert.IsType<StoredText.Unreadable>(_store.Files["save1.json"]);
        Assert.Equal(new StoredText.Found("garbage"), _store.Files["save2.json"]);
    }

    [Fact]
    public void AFailedWriteIsLoggedOnce()
    {
        _store.WriteFailure = "disk full";

        _ledger.Follow(true, "save1", 0);
        _ledger.Record(Make.Toast(1, "a"), null, null);
        _ledger.Flush(5, isForced: true);
        _ledger.Flush(6, isForced: true);

        Assert.Single(_log);
    }

    [Fact]
    public void ThePhoneIsToldOfEveryChangeAndOnlyOfChanges()
    {
        _ledger.Follow(true, "save1", 0);

        var start = _ledger.Revision;

        _ledger.Record(Make.Toast(1, "a"), null, null);

        var afterRecord = _ledger.Revision;

        _ledger.MarkSeen(Current().LatestMark);

        var afterSeen = _ledger.Revision;

        _ledger.MarkSeen(0);
        _ledger.Follow(true, "save1", 1);

        Assert.NotEqual(start, afterRecord);
        Assert.NotEqual(afterRecord, afterSeen);
        Assert.Equal(afterSeen, _ledger.Revision);
    }

    [Fact]
    public void ASaveNamedWithPlainCharactersKeepsItsName() => Assert.Equal("Save_03-a.json", SaveFileName.For("Save_03-a"));

    [Theory]
    [InlineData("../../evil")]
    [InlineData("C:\\saves\\one")]
    [InlineData("save.sav")]
    public void ASaveNameCannotReachOutsideTheHistoryFolder(string saveKey)
    {
        var name = SaveFileName.For(saveKey);

        Assert.Equal(name, Path.GetFileName(name));
        Assert.DoesNotContain("..", name.Substring(0, name.Length - ".json".Length));
        Assert.EndsWith(".json", name);
    }

    [Fact]
    public void TwoSavesWhoseNamesCleanUpTheSameGetTheirOwnFiles() => Assert.NotEqual(SaveFileName.For("a.b"), SaveFileName.For("a/b"));

    [Fact]
    public void AVeryLongSaveNameIsCut() => Assert.True(SaveFileName.For(new string('x', 300)).Length < 100);

    private History Current() => _ledger.Current ?? throw new InvalidOperationException("the ledger keeps no history");

    private static string Saved(string title)
    {
        var history = new History();

        history.Record(Make.Toast(1, title), null, null);

        return HistoryJson.Write(history);
    }

    [Fact]
    public void ReadingAndClearingAreSavedAndRedrawTheList()
    {
        _ledger.Follow(true, "save1", 0);
        _ledger.Record(Make.Toast(1, "a"), null, null);
        _ledger.Record(Make.Toast(2, "b"), null, null);

        var before = _ledger.Revision;

        _ledger.MarkRead(Current().Entries[0].Seq);

        Assert.True(_ledger.Revision > before);
        Assert.Equal(1, Current().UnreadCount);

        before = _ledger.Revision;
        _ledger.MarkRead(Current().Entries[0].Seq);

        Assert.Equal(before, _ledger.Revision);

        _ledger.Clear(Current().LatestMark);

        Assert.True(_ledger.Revision > before);
        Assert.Empty(Current().Entries);
    }
}
