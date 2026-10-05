using System.Collections.Concurrent;
using HeroesUnwelcomed.Game.Data;

namespace HeroesUnwelcomed.Game.DevTools;

/// <summary>
/// Watches the data folder and announces JSON files that changed, so the game
/// can reload them while running ("live reload").
/// </summary>
/// <remarks>
/// <para>
/// <b>Threads.</b> Windows reports file changes on a background thread, but
/// game objects must only be touched from the game's own thread. So the watcher
/// just drops paths into a thread-safe queue, and <see cref="Update"/> (on the
/// game thread) hands them out.
/// </para>
/// <para>
/// <b>Settling.</b> Saving one file often fires several events in a burst
/// (editors write, rename, then touch the file). We wait until no event has
/// arrived for a moment, then report each file once. Reading too early can catch
/// a half-written file.
/// </para>
/// </remarks>
public sealed class DataWatcher : IDisposable
{
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(200);

    private readonly DataSource _data;
    private readonly FileSystemWatcher? _watcher;
    private readonly ConcurrentQueue<string> _incoming = new();
    private readonly HashSet<string> _pending = [];
    private DateTime _lastEvent;

    public DataWatcher(DataSource data)
    {
        _data = data;
        if (!Directory.Exists(data.Root))
        {
            return;
        }

        _watcher = new FileSystemWatcher(data.Root, "*.json")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Renamed += OnFileEvent;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>The last file reported, and when (for the debug panel).</summary>
    public string? LastReloaded { get; private set; }

    public DateTime LastReloadedAt { get; private set; }

    /// <summary>Call every frame on the game thread.</summary>
    public void Update()
    {
        while (_incoming.TryDequeue(out string? path))
        {
            _pending.Add(path);
            _lastEvent = DateTime.UtcNow;
        }

        if (_pending.Count == 0 || DateTime.UtcNow - _lastEvent < SettleTime)
        {
            return;
        }

        foreach (string path in _pending)
        {
            _data.NotifyChanged(path);
            LastReloaded = path;
            LastReloadedAt = DateTime.Now;
        }
        _pending.Clear();
    }

    // Runs on a background thread: only touch the thread-safe queue here.
    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        string relative = Path.GetRelativePath(_data.Root, e.FullPath).Replace('\\', '/');
        _incoming.Enqueue(relative);
    }

    public void Dispose() => _watcher?.Dispose();
}
