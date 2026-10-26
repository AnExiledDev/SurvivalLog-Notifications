using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// One history file per save, in a folder of its own. Never throws: every failure comes back as its reason. Writes run
/// on a worker thread so the game's frame never waits on the disk; only the newest copy of each file is written.
/// </summary>
internal sealed class HistoryFiles : IHistoryStore
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    /// <summary>A read waits this long at most for a write of the file to finish.</summary>
    private static readonly TimeSpan ReadWait = TimeSpan.FromSeconds(5);

    private readonly string _folder;
    private readonly object _gate = new();
    private readonly Dictionary<string, HistorySnapshot> _waiting = new();
    private bool _isWriting;
    private string? _failure;

    public HistoryFiles(string folder) => _folder = folder;

    public StoredText Read(string fileName)
    {
        Settle(ReadWait);

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

    public string? Write(string fileName, HistorySnapshot snapshot)
    {
        lock (_gate)
        {
            _waiting[fileName] = snapshot;

            if (_isWriting) return null;

            _isWriting = true;
        }

        try
        {
            ThreadPool.QueueUserWorkItem(_ => WriteWaiting());

            return null;
        }
        catch (Exception ex)
        {
            lock (_gate)
            {
                _waiting.Remove(fileName);
                _isWriting = false;
                Monitor.PulseAll(_gate);
            }

            return ex.Message;
        }
    }

    public string? TakeFailure()
    {
        // Read every frame: the common case takes no lock.
        if (Volatile.Read(ref _failure) == null) return null;

        lock (_gate)
        {
            var failure = _failure;

            _failure = null;

            return failure;
        }
    }

    public string? SetAside(string fileName)
    {
        Settle(ReadWait);

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

    /// <summary>Waits until every write handed over is on disk, or the time is up. False when time ran out.</summary>
    public bool Settle(TimeSpan most)
    {
        var until = DateTime.UtcNow + most;

        lock (_gate)
        {
            while (_isWriting)
            {
                var left = until - DateTime.UtcNow;

                if (left <= TimeSpan.Zero || !Monitor.Wait(_gate, left)) return false;
            }
        }

        return true;
    }

    /// <summary>On the worker thread: writes what is waiting until nothing is. Never throws.</summary>
    private void WriteWaiting()
    {
        while (true)
        {
            string fileName;
            HistorySnapshot snapshot;

            lock (_gate)
            {
                if (_waiting.Count == 0)
                {
                    _isWriting = false;
                    Monitor.PulseAll(_gate);

                    return;
                }

                var first = _waiting.First();

                fileName = first.Key;
                snapshot = first.Value;
                _waiting.Remove(fileName);
            }

            if (WriteNow(fileName, snapshot) is { } failure)
            {
                lock (_gate) _failure = fileName + ": " + failure;
            }
        }
    }

    /// <summary>Written beside the file first and then swapped in, so a crash mid-write leaves the old file whole.</summary>
    private string? WriteNow(string fileName, HistorySnapshot snapshot)
    {
        try
        {
            var text = HistoryJson.Write(snapshot);
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

    private string PathOf(string fileName) => Path.Combine(_folder, fileName);
}
