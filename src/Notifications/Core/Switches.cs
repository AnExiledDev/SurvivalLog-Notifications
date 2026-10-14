using System.Collections.Generic;

namespace Notifications.Core;

/// <summary>The player's switches: which popups become toasts, and which game toasts the mod takes over.</summary>
public sealed record Switches(
    bool WishFulfilled,
    bool ItemsReceived,
    bool WaveReport,
    bool SkillLevelUp,
    bool DroneDelivery,
    bool PopText,
    bool SystemUnlock,
    bool CharacterUnlock,
    bool KeepGameToastLook)
{
    /// <summary>One line for the log.</summary>
    public string Describe()
    {
        var on = new List<string>();

        if (WishFulfilled) on.Add("wish");
        if (ItemsReceived) on.Add("items");
        if (WaveReport) on.Add("wave report");
        if (SkillLevelUp) on.Add("skill up");
        if (DroneDelivery) on.Add("drone delivery");

        var popups = on.Count == 0 ? "no popups" : "popups as toasts: " + string.Join(", ", on);

        on.Clear();

        if (PopText) on.Add("short texts");
        if (SystemUnlock) on.Add("system unlocks");
        if (CharacterUnlock) on.Add("survivor unlocks");

        var toasts = on.Count == 0 ? "no game toasts" : "game toasts: " + string.Join(", ", on) + (KeepGameToastLook ? " (kept in the game's look)" : "");

        return popups + "; " + toasts;
    }
}
