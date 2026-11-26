using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class SaveFollowerTests
{
    private readonly FakeStore _store = new();
    private readonly FakePlay _play = new();
    private readonly List<string> _log = new();
    private readonly Ledger _ledger;
    private readonly SaveFollower _follower;

    public SaveFollowerTests()
    {
        _ledger = new Ledger(_store, _log.Add);
        _follower = new SaveFollower(_ledger, _play, _log.Add);
    }

    [Fact]
    public void NothingIsReadBeforePlayStarts()
    {
        InPlay("save1");

        _follower.Tick(0);

        Assert.Equal(0, _play.Reads);
    }

    [Fact]
    public void AStartIsReadEachFrameUntilANamedSaveIsInPlay()
    {
        _follower.PlayStarted();

        _follower.Tick(0);
        InPlay(null);
        _follower.Tick(1);
        InPlay("save1");
        _follower.Tick(2);
        _follower.Tick(3);

        Assert.Equal(3, _play.Reads);
        Assert.Equal(new[] { "kept" }, KeptIn("save1.json"));
    }

    [Fact]
    public void EachStartIsReadAgain()
    {
        InPlay("save1");
        _follower.PlayStarted();
        _follower.Tick(0);

        InPlay("save2");
        _follower.Tick(1);
        _follower.PlayStarted();
        _follower.Tick(2);

        Assert.Equal(2, _play.Reads);
        Assert.Equal(new[] { "kept" }, KeptIn("save2.json"));
    }

    [Fact]
    public void WithoutTheStartHookEveryFrameIsRead()
    {
        InPlay("save1");
        _follower.CannotSeeStarts();

        _follower.Tick(0);
        _follower.Tick(1);
        _follower.Tick(2);

        Assert.Equal(3, _play.Reads);
    }

    [Fact]
    public void ANoticeGoesToTheSaveInPlayEvenWithoutAStart()
    {
        InPlay("save1");
        _follower.PlayStarted();
        _follower.Tick(0);
        InPlay("save2");

        _follower.BeforeNotice(1);

        Assert.Equal(new[] { "kept" }, KeptIn("save2.json"));
    }

    /// <summary>On the title screen there is no history, so a notice there is dropped, and it does not start reading every frame.</summary>
    [Fact]
    public void ANoticeOutOfAGameKeepsNothingAndStartsNoReads()
    {
        InPlay("save1");
        _follower.PlayStarted();
        _follower.Tick(0);
        _play.InGame = false;

        _follower.BeforeNotice(1);
        _follower.Tick(2);

        Assert.Null(_ledger.Record(Make.Toast(1, "dropped"), null, null));
        Assert.Equal(2, _play.Reads);
    }

    [Fact]
    public void AFrameWhoseReadFailsIsReadAgainNextFrame()
    {
        _follower.PlayStarted();
        _play.Failure = new InvalidOperationException("not ready");

        Assert.Throws<InvalidOperationException>(() => _follower.Tick(0));

        _play.Failure = null;
        InPlay("save1");
        _follower.Tick(1);

        Assert.Equal(new[] { "kept" }, KeptIn("save1.json"));
    }

    [Fact]
    public void AReadThatFailsBeforeANoticeIsLoggedOnce()
    {
        _play.Failure = new InvalidOperationException("not ready");

        _follower.BeforeNotice(0);
        _follower.BeforeNotice(1);

        Assert.Single(_log, line => line.Contains("not ready"));
    }

    private void InPlay(string? saveKey)
    {
        _play.InGame = true;
        _play.Key = saveKey;
    }

    /// <summary>Keeps a notice and saves at once, then reads back the titles saved in this file.</summary>
    private string[]? KeptIn(string fileName)
    {
        _ledger.Record(Make.Toast(1, "kept"), null, null);
        _ledger.Flush(100, true);

        return _store.SavedTitles(fileName);
    }
}

/// <summary>Where play is, set by the test. It counts the reads, since each one is a call into the game.</summary>
internal sealed class FakePlay : IPlayState
{
    public bool InGame { get; set; }

    public string? Key { get; set; }

    public Exception? Failure { get; set; }

    public int Reads { get; private set; }

    public bool IsInGame()
    {
        Reads++;

        return Failure == null ? InGame : throw Failure;
    }

    public string? SaveKey() => Key;
}
