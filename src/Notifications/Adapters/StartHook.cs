using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// Tells the save follower when play starts. AgentManager.StartBattle (0x310c360) runs at every start of play: a load, a
/// new game, a trip and the tutorial all reach it through BattleLogicWorld.StartBattle, its only caller. Its first 14
/// bytes have no relative jump or call. The hook runs in the middle of the game's load, so it only marks the start, and
/// the ticker reads the save on the frames after.
/// </summary>
internal static class StartHook
{
    private static SaveFollower? _follower;

    public static void Apply(PatchKit hooks, SaveFollower follower)
    {
        _follower = follower;

        if (!hooks.Patch(typeof(StartHook), typeof(AgentManager), nameof(AgentManager.StartBattle), new[] { typeof(GameSaveData), typeof(GameMode) },
                postfix: nameof(AfterStart), consequence: "which save is in play is read every frame instead"))
        {
            follower.CannotSeeStarts();
        }
    }

    private static void AfterStart() => _follower?.PlayStarted();
}
