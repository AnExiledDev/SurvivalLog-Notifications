using System;
using System.Collections.Generic;
using ExiledToolkit.Kit;
using Notifications.Core;
using UnityEngine;

namespace Notifications.Adapters;

/// <summary>
/// Each frame: follows which save is in play when it may have changed, writes the history when it is due, talks to the
/// phone app, and closes the popups a toast stood in for. A failure is logged the first time it is seen and the next
/// frame carries on. Each step is called directly rather than through a delegate, so a frame allocates nothing here.
/// </summary>
public sealed class NoteTicker : MonoBehaviour
{
    private const int MostFailuresLogged = 20;

    internal static Ledger? Ledger;
    internal static SaveFollower? Follower;
    internal static PhoneApp? Phone;
    internal static PopupCloser? Closer;
    internal static Action<string> Log = _ => { };

    /// <summary>Waits for history writes still under way, so quitting does not cut one off.</summary>
    internal static Action Settle = () => { };

    private static readonly HashSet<string> ReportedFailures = new();

    public NoteTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        var now = Clock.Now;

        if (Follower is { } follower)
        {
            try
            {
                follower.Tick(now);
            }
            catch (Exception ex)
            {
                Report("following the save", ex);
            }
        }

        if (Ledger is { } ledger)
        {
            try
            {
                ledger.Flush(now);
            }
            catch (Exception ex)
            {
                Report("saving the history", ex);
            }
        }

        if (Phone is { } phone)
        {
            try
            {
                phone.Tick(now);
            }
            catch (Exception ex)
            {
                Report("updating the phone app", ex);
            }
        }

        if (Closer is { } closer)
        {
            try
            {
                closer.Tick();
            }
            catch (Exception ex)
            {
                Report("closing popups a toast stood in for", ex);
            }
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            if (Ledger is { } ledger) ledger.Flush(Clock.Now, true);

            Settle();
        }
        catch (Exception ex)
        {
            Report("saving the history on quit", ex);
        }
    }

    private static void Report(string doing, Exception ex)
    {
        if (ReportedFailures.Count < MostFailuresLogged && ReportedFailures.Add(doing + ex.GetType().Name + ex.Message)) Log($"{doing} failed (each failure logged once): {ex}");
    }
}
