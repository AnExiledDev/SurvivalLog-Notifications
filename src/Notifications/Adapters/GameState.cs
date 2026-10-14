using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>Where the game is, read fresh each time. Every read is total: a failure reads as "not known".</summary>
internal static class GameState
{
    /// <summary>A survivor is in a world; on the title screen and while a save loads there is none.</summary>
    public static bool IsInGame()
    {
        try
        {
            return GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole() != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>The loaded save's file name, or null before the game has named it.</summary>
    public static string? SaveKey()
    {
        try
        {
            var name = GameSingleton.Existing<GameSaveManager>()?.gameSave?.FileName;

            return string.IsNullOrEmpty(name) ? null : name;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>The in-game day and hour, by the game's own clock.</summary>
    public static GameStamp? Stamp()
    {
        try
        {
            var time = GameSingleton.Existing<BattleLogicWorld>()?._GameTimeManager;

            return time == null ? null : new GameStamp(time.GetDay(), time.GetHour24());
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool IsChinese()
    {
        try
        {
            return GameSingleton.Existing<SettingManager>()?.IsChinese() == true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Whether this trade manager is the live world's, not a leftover one still answering the game's events.</summary>
    public static bool IsLive(StrangerTradeActiveManager trade) =>
        GameSingleton.Existing<BattleLogicWorld>()?._StrangerTradeActiveManager?.Pointer == trade.Pointer;
}
