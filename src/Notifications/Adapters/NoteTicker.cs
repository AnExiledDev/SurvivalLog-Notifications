using System;
using System.Collections.Generic;
using Notifications.Core;
using UnityEngine;

namespace Notifications.Adapters;

/// <summary>
/// Each frame: follows which save is loaded, writes the history when it is due, and talks to the phone app. A failure
/// is logged the first time it is seen and the next frame carries on.
/// </summary>
public sealed class NoteTicker : MonoBehaviour
{
    private const int MostFailuresLogged = 20;

    internal static Ledger? Ledger;
    internal static PhoneApp? Phone;
    internal static Action<string> Log = _ => { };

    private static readonly HashSet<string> ReportedFailures = new();

    public NoteTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        var now = (double)Time.unscaledTime;

        if (Ledger is { } ledger)
        {
            Step("following the save", () => ledger.Follow(GameState.IsInGame(), GameState.SaveKey(), now));
            Step("saving the history", () => ledger.Flush(now));
        }

        if (Phone is { } phone) Step("updating the phone app", () => phone.Tick(now));
    }

    private void OnApplicationQuit()
    {
        if (Ledger is { } ledger) Step("saving the history on quit", () => ledger.Flush(Time.unscaledTime, true));
    }

    private static void Step(string doing, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            if (ReportedFailures.Count < MostFailuresLogged && ReportedFailures.Add(doing + ex.GetType().Name + ex.Message)) Log($"{doing} failed (each failure logged once): {ex}");
        }
    }
}
