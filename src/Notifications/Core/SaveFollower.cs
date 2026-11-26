using System;

namespace Notifications.Core;

/// <summary>Where play is, read from the game. Each read is a call into the game.</summary>
public interface IPlayState
{
    bool IsInGame();

    /// <summary>The save in play, or null while it has no name.</summary>
    string? SaveKey();
}

/// <summary>
/// Tells the history which save is in play. Only a start of play (a load, a new game, a trip or the tutorial) changes
/// the save, so it is read each frame from a start until a named save is in play, then not again until the next start.
/// It is also read once before each notice is kept, so a notice never goes to the wrong save. If the hook that sees a
/// start could not go in, it is read every frame. Main thread only.
/// </summary>
public sealed class SaveFollower
{
    private readonly Ledger _ledger;
    private readonly IPlayState _play;
    private readonly Action<string> _log;
    private bool _isStarting;
    private bool _isBlind;
    private bool _hasReportedFailure;

    public SaveFollower(Ledger ledger, IPlayState play, Action<string> log)
    {
        _ledger = ledger;
        _play = play;
        _log = log;
    }

    /// <summary>Play started, so the save in play may be another one.</summary>
    public void PlayStarted() => _isStarting = true;

    /// <summary>The hook that sees a start of play could not go in, so nothing says when the save changes.</summary>
    public void CannotSeeStarts() => _isBlind = true;

    /// <summary>Each frame. A read that fails is the caller's to report, and the next frame reads again.</summary>
    public void Tick(double now)
    {
        if (_isStarting || _isBlind) Follow(now);
    }

    /// <summary>Before a notice is kept, so it goes to the save in play even when no start was seen. Never throws.</summary>
    public void BeforeNotice(double now)
    {
        try
        {
            Follow(now);
        }
        catch (Exception ex)
        {
            if (!_hasReportedFailure) _log($"could not check which save is in play before keeping a notice (logged once): {ex}");

            _hasReportedFailure = true;
        }
    }

    private void Follow(double now)
    {
        var isInGame = _play.IsInGame();
        // Out of a game the history does not need the name, and reading it makes a new string.
        var saveKey = isInGame ? _play.SaveKey() : null;

        _ledger.Follow(isInGame, saveKey, now);

        // Once a named save is in play, the next change can only come with the next start.
        if (isInGame && saveKey != null) _isStarting = false;
    }
}
