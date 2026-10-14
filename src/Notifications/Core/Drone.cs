using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Notifications.Core;

/// <summary>The rules for a drone coming home with cargo.</summary>
public static class DroneCargo
{
    /// <summary>
    /// The same test Storage Hub uses for a drone that is about to hand over cargo: the trade post is open, the trip is
    /// under way, it is not a charity run, and it carries something.
    /// </summary>
    public static bool HandsOver(bool isTradeLocked, bool isTripBusy, bool isCharity, string? cargoJson) =>
        !isTradeLocked && isTripBusy && !isCharity && !string.IsNullOrEmpty(cargoJson);

    /// <summary>
    /// The trip's cargo, written by the game as {"itemId":count}. Counts of the same item are added, nothing that is
    /// not a positive count of a numbered item is kept, and text that is not that shape is no cargo.
    /// </summary>
    public static IReadOnlyList<ItemStack> Parse(string? cargoJson)
    {
        var counts = new Dictionary<int, int>();
        var order = new List<int>();

        try
        {
            using var document = JsonDocument.Parse(cargoJson ?? "");

            if (document.RootElement.ValueKind != JsonValueKind.Object) return Array.Empty<ItemStack>();

            foreach (var item in document.RootElement.EnumerateObject())
            {
                if (!int.TryParse(item.Name, out var id) || id <= 0 || !item.Value.TryGetInt32(out var count) || count <= 0) continue;

                if (!counts.ContainsKey(id)) order.Add(id);

                counts[id] = counts.TryGetValue(id, out var before) ? before + count : count;
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return Array.Empty<ItemStack>();
        }

        return order.Select(id => new ItemStack(id, counts[id])).ToList();
    }

    /// <summary>Whether every line of a popup is cargo the drone brought, no more of each item than it carried.</summary>
    public static bool Covers(IReadOnlyList<ItemStack> cargo, IReadOnlyList<ItemStack> shown)
    {
        if (shown.Count == 0) return false;

        var left = new Dictionary<int, int>();

        foreach (var stack in cargo) left[stack.ConfigId] = left.TryGetValue(stack.ConfigId, out var before) ? before + stack.Count : stack.Count;

        foreach (var stack in shown)
        {
            if (!left.TryGetValue(stack.ConfigId, out var carried) || stack.Count > carried) return false;

            left[stack.ConfigId] = carried - stack.Count;
        }

        return true;
    }

    /// <summary>Whether a popup shows all of the cargo and nothing else, in any order.</summary>
    public static bool IsExactly(IReadOnlyList<ItemStack> cargo, IReadOnlyList<ItemStack> shown) => Covers(cargo, shown) && Covers(shown, cargo);
}

/// <summary>
/// Drones that came home and whose unload popup has not been seen yet. The game shows the drone's cargo in the
/// received-items popup with its unload line; that popup is the drone toast's to replace, and only that one.
/// </summary>
public sealed class DroneAwait
{
    public const double WaitSeconds = 30;

    private readonly List<Arrival> _arrivals = new();

    /// <summary>A drone is handing over this cargo now.</summary>
    public Arrival Expect(IReadOnlyList<ItemStack> cargo, double now)
    {
        _arrivals.RemoveAll(arrival => arrival.Until < now);

        var arrival = new Arrival(cargo, now + WaitSeconds);

        _arrivals.Add(arrival);

        return arrival;
    }

    /// <summary>
    /// The waiting drone a received-items popup is the unload of: the popup carries the unload line and only that
    /// drone's cargo. A drone whose cargo is exactly the popup's comes first, then the one that came home first among
    /// those that carried all of it, so drones with overlapping cargo each get their own list. That drone stops
    /// waiting and keeps the popup's data. Null when the popup is no drone's.
    /// </summary>
    public Arrival? Claim(Reopen.Items popup, string? unloadDesc, double now)
    {
        _arrivals.RemoveAll(arrival => arrival.Until < now);

        if (string.IsNullOrEmpty(unloadDesc) || NoteTexts.Plain(popup.Desc) != NoteTexts.Plain(unloadDesc)) return null;

        var arrival = _arrivals.Find(waiting => DroneCargo.IsExactly(waiting.Cargo, popup.Stacks)) ??
                      _arrivals.Find(waiting => DroneCargo.Covers(waiting.Cargo, popup.Stacks));

        if (arrival == null) return null;

        _arrivals.Remove(arrival);
        arrival.Popup = popup;

        return arrival;
    }

    /// <summary>
    /// The drone did not land after all, or its toast could not be shown, so its popup stays with the player: it stops
    /// waiting, and a popup it already claimed comes back to be left up or opened again.
    /// </summary>
    public Reopen.Items? GiveUp(Arrival arrival)
    {
        _arrivals.Remove(arrival);

        return arrival.Popup;
    }

    public sealed class Arrival
    {
        public Arrival(IReadOnlyList<ItemStack> cargo, double until)
        {
            Cargo = cargo;
            Until = until;
        }

        public IReadOnlyList<ItemStack> Cargo { get; }

        public double Until { get; }

        /// <summary>The Toolkit's id for this drone's toast.</summary>
        public long ToastId { get; set; }

        /// <summary>The unload popup this drone's toast stands for, once the game sent it.</summary>
        public Reopen.Items? Popup { get; set; }
    }
}

/// <summary>What the mod does with one of the game's own small toasts.</summary>
public enum GameToastHandling
{
    /// <summary>The game shows it and the history does not take it.</summary>
    Leave,

    /// <summary>The game shows it, and the history takes a copy.</summary>
    HistoryOnly,

    /// <summary>A Toolkit toast shows it instead, which the history takes like any toast.</summary>
    Replace,
}

/// <summary>The decisions about game toasts and popups that hang on the player's switches.</summary>
public static class Handling
{
    /// <summary>
    /// A group switched off is left to the game. Keeping the game's look, or having the Toolkit's toasts switched off,
    /// lets the game draw it and copies it into the history, as a toast nobody could see must not replace it.
    /// </summary>
    public static GameToastHandling GameToast(bool isGroupOn, bool isKeepingGameLook, bool areToastsShown)
    {
        if (!isGroupOn) return GameToastHandling.Leave;

        return isKeepingGameLook || !areToastsShown ? GameToastHandling.HistoryOnly : GameToastHandling.Replace;
    }

    /// <summary>
    /// A popup becomes a toast only when its switch is on and toasts are on screen; otherwise the game opens it, and
    /// with toasts off the history still takes it, clickable to open it again.
    /// </summary>
    public static GameToastHandling Popup(bool isSwitchOn, bool areToastsShown)
    {
        if (!isSwitchOn) return GameToastHandling.Leave;

        return areToastsShown ? GameToastHandling.Replace : GameToastHandling.HistoryOnly;
    }
}
