using System;
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
        IsChinese = GameState.IsChinese();
        _icons = icons ?? IconIndex.Empty;
    }

    public bool IsChinese { get; }

    public string? Constant(string id)
    {
        try
        {
            return NonEmpty(ConstantTextTools.ToConstantTextOrEmpty(id));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string? ItemName(int configId) =>
        Read(config => config.Get_Config_Item(configId) is { } item ? Localized(config, item.ItemName) ?? NonEmpty(item.ItemName_Local) : null);

    public string? ItemIcon(int configId) =>
        _icons.Resolve(Read(config => config.Get_Config_Item(configId) is { } item ? NonEmpty(item.WebIcon) ?? NonEmpty(item.Icon) : null));

    public string? BuffName(int buffId) =>
        buffId <= 0 ? null : Read(config => config.Get_Config_Buff(buffId) is { } buff ? Localized(config, buff.Name) ?? NonEmpty(buff.Name_Local) : null);

    public string? SurvivorName(int playerSelectId) =>
        Read(config => config.Get_Config_PlayerSelect(playerSelectId) is { } survivor ? Localized(config, survivor.Name) ?? NonEmpty(survivor.Name_Local) : null);

    /// <summary>A system's name and the hint the game shows when it unlocks.</summary>
    public (string? Name, string? Tips) SystemLock(int systemId)
    {
        string? tips = null;
        var name = Read(config =>
        {
            if (config.Get_Config_SystemLock(systemId) is not { } system) return null;

            tips = Localized(config, system.Tips) ?? NonEmpty(system.Tips_Local);

            return Localized(config, system.SysName) ?? NonEmpty(system.SysName_Local);
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

    /// <summary>The game answers a key it has no text for with the key itself, which is no text either.</summary>
    private static string? Localized(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var local = config.GetLocalTxt(key);

        return string.IsNullOrEmpty(local) || local == key ? null : local;
    }

    private static string? NonEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;
}
