// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// SQLite implementation of <see cref="IDatabaseChangeNotifier"/> using file system watcher.
/// Since SQLite doesn't have built-in notification support, we watch the database file for changes.
/// </summary>
/// <param name="databasePath">The path to the SQLite database file.</param>
/// <param name="logger">The logger.</param>
public sealed class SqliteChangeNotifier(string databasePath, ILogger<SqliteChangeNotifier> logger) : IDatabaseChangeNotifier
{
    /// <summary>
    /// Debounce interval to prevent multiple notifications for a single change.
    /// </summary>
    static readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(100);

    readonly object _lock = new();
    FileSystemWatcher? _watcher;
    Action? _onChanged;
    DateTime _lastNotification = DateTime.MinValue;
    bool _disposed;

    /// <inheritdoc/>
    public Task StartListeningAsync(string tableName, Action onChanged, CancellationToken cancellationToken = default)
    {
        _onChanged = onChanged;

        var directory = Path.GetDirectoryName(databasePath);
        var fileName = Path.GetFileName(databasePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            logger.SqliteDirectoryNotFound(directory ?? "null");
            return Task.CompletedTask;
        }

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Error += OnWatcherError;

        logger.StartedListeningSqlite(databasePath);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopListeningAsync()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }

        logger.StoppedListeningSqlite(databasePath);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopListeningAsync();
    }

    void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce to prevent multiple notifications for a single change
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastNotification < _debounceInterval)
            {
                return;
            }
            _lastNotification = now;
        }

        logger.SqliteFileChanged(e.FullPath, e.ChangeType.ToString());
        _onChanged?.Invoke();
    }

    void OnWatcherError(object sender, ErrorEventArgs e)
    {
        logger.SqliteWatcherError(e.GetException());
    }
}
