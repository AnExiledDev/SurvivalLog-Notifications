using System;
using System.Threading.Tasks;
using ExiledToolkit.Web;

namespace Notifications.Adapters;

/// <summary>
/// The game's web icons, so notices can show the same pictures, from the Toolkit's one scan of the icon folder. The
/// scan starts at load on a worker, so it is nearly always done by the first notice. Main thread only.
/// </summary>
internal static class NoteIcons
{
    private static Task<(IconIndex Icons, string Note)>? _scan;
    private static bool _hasLoggedScan;

    /// <summary>At load, on the main thread.</summary>
    public static void StartScan() => _scan = WebIcons.StartScan();

    /// <summary>
    /// The first call waits for a scan still under way, which is never longer than reading the folder there and then,
    /// so no notice goes without its pictures; it logs what the scan found. The scan never fails.
    /// </summary>
    public static IconIndex Get(Action<string> log)
    {
        var scan = _scan ??= WebIcons.StartScan();

        if (_hasLoggedScan) return scan.Result.Icons;

        var (icons, note) = scan.Result;

        _hasLoggedScan = true;
        log(note);

        return icons;
    }
}
