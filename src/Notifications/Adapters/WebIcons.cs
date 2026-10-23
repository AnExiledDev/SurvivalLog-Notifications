using System;
using System.IO;
using System.Linq;
using Notifications.Core;
using UnityEngine;

namespace Notifications.Adapters;

/// <summary>Indexes the icon files the game ships for its web UI, so notices can show the same pictures.</summary>
internal static class WebIcons
{
    private static IconIndex? _shared;

    /// <summary>Read from disk once, on first need.</summary>
    public static IconIndex Shared(Action<string> log) => _shared ??= Load(log);

    private static IconIndex Load(Action<string> log)
    {
        try
        {
            var webUi = Path.Combine(Application.streamingAssetsPath, "WebUI");
            var res = Path.Combine(webUi, "Res");
            var files = Directory.EnumerateFiles(res, "*.png", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(webUi, file)).ToList();

            log($"{files.Count} web icons found under {res}");

            return IconIndex.Build(files);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log($"no web icons, notices will show their kind's symbol instead of item pictures: {ex.Message}");

            return IconIndex.Empty;
        }
    }
}
