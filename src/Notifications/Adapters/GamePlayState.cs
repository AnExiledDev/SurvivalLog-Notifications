using ExiledToolkit.Game;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>Where play is, read fresh from the game each time.</summary>
internal sealed class GamePlayState : IPlayState
{
    public bool IsInGame() => GameState.IsInGame();

    public string? SaveKey() => SaveSlot.CurrentKey();
}
