using System;
using System.Globalization;
using System.IO;
using System.Text;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>One history file per save, in a folder of its own. Never throws: every failure comes back as its reason.</summary>
internal sealed class HistoryFiles : IHistoryStore
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    private readonly string _folder;

    public HistoryFiles(string folder) => _folder = folder;

    public StoredText Read(string fileName)
    {
        try
        {
            var path = PathOf(fileName);

            return File.Exists(path) ? new StoredText.Found(File.ReadAllText(path, Utf8)) : new StoredText.Missing();
        }
        catch (Exception ex)
        {
            return new StoredText.Unreadable(ex.Message);
        }
    }

    /// <summary>Written beside the file first and then swapped in, so a crash mid-write leaves the old file whole.</summary>
    public string? Write(string fileName, string text)
    {
        try
        {
            var path = PathOf(fileName);
            var written = path + ".tmp";

            Directory.CreateDirectory(_folder);
            File.WriteAllText(written, text, Utf8);

            if (File.Exists(path)) File.Replace(written, path, null);
            else File.Move(written, path);

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public string? SetAside(string fileName)
    {
        try
        {
            var path = PathOf(fileName);

            File.Move(path, path + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".bad");

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private string PathOf(string fileName) => Path.Combine(_folder, fileName);
}
