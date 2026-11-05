using System;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using GameCore.HotUpdate;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// The game's own words for a notice, read from its tables in the language it is set to. A line the game has no text
/// for is null, and the core falls back to its own.
/// </summary>
internal sealed class GameWords : IViewWords
{
    private readonly IconIndex _icons;

    /// <param name="icons">The game's web icons; without them no item has a picture.</param>
    public GameWords(IconIndex? icons = null)
    {
        IsChinese = GameLanguage.IsChinese();
        _icons = icons ?? IconIndex.Empty;
    }

    public bool IsChinese { get; }

    public string? Constant(string id)
    {
        try
        {
            return GameNames.NonEmpty(ConstantTextTools.ToConstantTextOrEmpty(id));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string? ItemName(int configId) =>
        Read(config => config.Get_Config_Item(configId) is { } item ? GameNames.Translate(config, item.ItemName) ?? GameNames.NonEmpty(item.ItemName_Local) : null);

    public string? ItemIcon(int configId) =>
        _icons.Resolve(Read(config => config.Get_Config_Item(configId) is { } item ? GameNames.NonEmpty(item.WebIcon) ?? GameNames.NonEmpty(item.Icon) : null));

    public string? BuffName(int buffId) =>
        buffId <= 0 ? null : Read(config => config.Get_Config_Buff(buffId) is { } buff ? GameNames.Translate(config, buff.Name) ?? GameNames.NonEmpty(buff.Name_Local) : null);

    public string? SurvivorName(int playerSelectId) =>
        Read(config => config.Get_Config_PlayerSelect(playerSelectId) is { } survivor ? GameNames.Translate(config, survivor.Name) ?? GameNames.NonEmpty(survivor.Name_Local) : null);

    /// <summary>A system's name and the hint the game shows when it unlocks.</summary>
    public (string? Name, string? Tips) SystemLock(int systemId)
    {
        string? tips = null;
        var name = Read(config =>
        {
            if (config.Get_Config_SystemLock(systemId) is not { } system) return null;

            tips = GameNames.Translate(config, system.Tips) ?? GameNames.NonEmpty(system.Tips_Local);

            return GameNames.Translate(config, system.SysName) ?? GameNames.NonEmpty(system.SysName_Local);
        });

        return (name, tips);
    }

    /// <summary>A table read that fails, as an unknown id can, is no text.</summary>
    private static string? Read(Func<ConfigManager, string?> read)
    {
        try
        {
            return GameSingleton.Existing<ConfigManager>() is { } config ? read(config) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
