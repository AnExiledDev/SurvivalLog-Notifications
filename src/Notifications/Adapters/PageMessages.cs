using System;
using System.Collections.Concurrent;
using HarmonyLib;
using Notifications.Core;
using Vuplex.WebView.Internal;

namespace Notifications.Adapters;

/// <summary>
/// Catches the phone app's messages before the game's web layer sees them (it would log them as unknown) and queues
/// them for the next frame. Static because a Harmony prefix has nowhere else to look.
/// </summary>
internal static class PageMessages
{
    private static readonly ConcurrentQueue<PageMessage> Queue = new();
    // Handed out when the queue is empty, which is nearly every frame, so that frame allocates nothing.
    private static readonly PageMessage Nothing = new PageMessage.Unknown("");
    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        HookPatch.Apply(harmony, AccessTools.Method(typeof(BaseWebView), "HandleMessageEmitted"), typeof(PageMessages), nameof(TakeOurs), null,
            "listening for the phone app's messages", "clicking a row of the phone's list does nothing", log);
    }

    public static bool TryTake(out PageMessage message)
    {
        if (Queue.TryDequeue(out var queued))
        {
            message = queued;

            return true;
        }

        message = Nothing;

        return false;
    }

    /// <summary>A throw here would break every web message in the game, so any failure lets the game handle the message.</summary>
    private static bool TakeOurs(string serializedMessage)
    {
        try
        {
            if (!PageMessage.IsOurs(serializedMessage)) return true;

            Queue.Enqueue(PageMessage.Parse(serializedMessage));

            return false;
        }
        catch (Exception ex)
        {
            ReportFailureOnce(ex);

            return true;
        }
    }

    private static void ReportFailureOnce(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;

        _log("reading a phone app message failed, the game handles it instead: " + ex.Message);
    }
}
