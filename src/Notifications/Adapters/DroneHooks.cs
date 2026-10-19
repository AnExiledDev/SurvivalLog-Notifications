using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Notifications.Core;
using UnityEngine;

namespace Notifications.Adapters;

/// <summary>
/// A drone coming home raises "Drone delivery arrived", and the game's unload popup for its cargo is closed once it is
/// up: the toast opens it again when clicked. The popup may come during the settle or after it, so the toast is raised
/// after the settle and given the popup whenever the popup turns up. Static because a Harmony patch has nowhere else to
/// look.
/// </summary>
internal static class DroneHooks
{
    private const string UnloadDescId = "SR_Web_DroneHub_UnloadDesc";

    private static readonly DroneAwait Await = new();
    private static NoticeDesk? _desk;
    private static DroneAwait.Arrival? _settling;
    private static SwitchedHook? _settle;

    public static void Apply(Harmony harmony, NoticeDesk desk, Action<string> log)
    {
        _desk = desk;

        // 0x2fc2310; the settle that hands the cargo over. The load-time restore is left alone.
        _settle = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(StrangerTradeActiveManager), nameof(StrangerTradeActiveManager.SettleDroneReturn)),
            typeof(DroneHooks), nameof(BeforeSettle), nameof(AfterSettle), "drone deliveries become toasts", "their unload popups stay popups", log));
    }

    /// <summary>Called on the main thread at start and after each change of a switch.</summary>
    public static void Use(Switches switches) => _settle?.Use(switches.DroneDelivery);

    /// <summary>
    /// Whether a received-items popup is a waiting drone's unload, which the drone's toast then stands for, so the popup
    /// is to be closed once up. False lets the popup go its usual way.
    /// </summary>
    public static bool TakeUnload(Reopen.Items items)
    {
        if (_desk is not { } desk) return false;

        try
        {
            if (Await.Claim(items, ConstantTextTools.ToConstantTextOrEmpty(UnloadDescId), Time.unscaledTime) is not { } arrival) return false;

            if (arrival.ToastId != 0) desk.Attach(arrival.ToastId, items);

            return true;
        }
        catch (Exception ex)
        {
            desk.Fail("matching a received-items popup to a drone", ex);

            return false;
        }
    }

    private static void BeforeSettle(StrangerTradeActiveManager __instance, DroneTripComponent trip)
    {
        _settling = null;

        if (_desk is not { } desk || trip == null) return;

        try
        {
            if (!GameState.IsLive(__instance) || trip.ReturnSettled) return;

            if (!DroneCargo.HandsOver(__instance.IsLocked, trip.IsBusy(), trip.IsCharity, trip.CargoJson)) return;

            if (desk.ForPopup(desk.Switches.DroneDelivery) != GameToastHandling.Replace) return;

            var cargo = DroneCargo.Parse(trip.CargoJson);

            if (cargo.Count > 0) _settling = Await.Expect(cargo, Time.unscaledTime);
        }
        catch (Exception ex)
        {
            desk.Fail("reading a returning drone", ex);
        }
    }

    /// <summary>Runs whether or not the settle ran; a drone that did not land leaves its popup up.</summary>
    private static void AfterSettle(DroneTripComponent trip)
    {
        var arrival = _settling;

        _settling = null;

        if (arrival == null || _desk is not { } desk) return;

        try
        {
            if (trip == null || !trip.ReturnSettled)
            {
                Release(desk, arrival);

                return;
            }

            var id = desk.Raise(NoteTexts.Drone(new GameWords(), arrival.Cargo), arrival.Popup, () => OpenUnload(desk, arrival));

            if (id == 0) Release(desk, arrival);
            else arrival.ToastId = id;
        }
        catch (Exception ex)
        {
            desk.Fail("raising a drone delivery toast", ex);
            Release(desk, arrival);
        }
    }

    /// <summary>A drone whose cargo did not fit unloads into its own bag and shows no popup, so its toast opens nothing.</summary>
    private static void OpenUnload(NoticeDesk desk, DroneAwait.Arrival arrival)
    {
        if (arrival.Popup is { } popup) desk.OpenPopup(popup);
    }

    /// <summary>
    /// A popup already claimed was claimed in this same settle, so its close is still pending and dropping it leaves the
    /// popup up; one already closed is opened again.
    /// </summary>
    private static void Release(NoticeDesk desk, DroneAwait.Arrival arrival)
    {
        if (Await.GiveUp(arrival) is { } popup && !desk.KeepOpen(PopupKind.Items)) desk.OpenPopup(popup);
    }
}
