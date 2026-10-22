// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Owns the reconnecting change stream for one resolved database.
/// </summary>
/// <param name="database">The database to watch.</param>
/// <param name="logger">The shared watch logger.</param>
/// <param name="token">The watcher lifetime.</param>
internal sealed class MongoDBDatabaseWatch(IMongoDatabase database, ILogger<MongoDBWatcher> logger, CancellationToken token)
    : IObservable<ChangeStreamDocument<BsonDocument>>
{
    readonly object _openGate = new();
    TaskCompletionSource _opened = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Determines whether a driver failure requires a new snapshot rather than resuming.
    /// </summary>
    /// <param name="error">The driver failure.</param>
    /// <returns>Whether the resume token must be discarded.</returns>
    internal static bool HasInvalidResumeToken(Exception error) => error is MongoCommandException command &&
        (command.Code is 286 or 260 or 280 ||
        string.Equals(command.CodeName, "ChangeStreamHistoryLost", StringComparison.Ordinal) ||
        string.Equals(command.CodeName, "InvalidResumeToken", StringComparison.Ordinal) ||
        string.Equals(command.CodeName, "ChangeStreamFatalError", StringComparison.Ordinal));

    /// <summary>
    /// Gets the watched database.
    /// </summary>
    internal IMongoDatabase Database { get; } = database;

    /// <summary>
    /// Gets the serialized stream of changes and resynchronization notifications.
    /// </summary>
    internal ISubject<ChangeStreamDocument<BsonDocument>> Changes { get; } = Subject.Synchronize(new Subject<ChangeStreamDocument<BsonDocument>>());

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<ChangeStreamDocument<BsonDocument>> observer) => Changes.Subscribe(observer);

    /// <summary>
    /// Waits for the current connection to open before an observation reads its snapshot.
    /// </summary>
    /// <param name="cancellationToken">The observation lifetime.</param>
    /// <returns>A task that completes once the current cursor is open.</returns>
    internal Task WhenOpen(CancellationToken cancellationToken)
    {
        lock (_openGate)
        {
            return _opened.Task.WaitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Watches until canceled, reopening and resynchronizing when necessary.
    /// </summary>
    /// <param name="databaseName">The database name for diagnostics.</param>
    /// <returns>The lifetime of the watch.</returns>
    internal async Task Run(string databaseName)
    {
        var delay = TimeSpan.FromSeconds(1);
        BsonDocument? resumeToken = null;
        var resynchronize = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var options = new ChangeStreamOptions
                    {
                        FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                        ResumeAfter = resumeToken
                    };
                    var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
                    using var cursor = await Database.WatchAsync(pipeline, options, token);
                    lock (_openGate)
                    {
                        _opened.TrySetResult();
                    }
                    logger.StartedWatchingDatabase(databaseName);
                    if (resynchronize)
                    {
                        // Reopen before asking for snapshots, so writes during the queries remain in the stream.
                        Publish(new ChangeStreamDocument<BsonDocument>(new BsonDocument("operationType", "invalidate"), BsonDocumentSerializer.Instance));
                        resynchronize = false;
                    }

                    var invalidated = false;
                    while (!invalidated && await cursor.MoveNextAsync(token))
                    {
                        foreach (var change in cursor.Current)
                        {
                            if (change.OperationType == ChangeStreamOperationType.Invalidate)
                            {
                                resumeToken = null;
                                resynchronize = true;
                                invalidated = true;
                                break;
                            }

                            resumeToken = change.ResumeToken;
                            Publish(change);
                            delay = TimeSpan.FromSeconds(1);
                        }
                    }

                    logger.DatabaseWatchCompleted(databaseName);

                    // Invalidation and exhaustion both back off, so repeated invalidations cannot spin.
                    // An exhausted cursor is also a reconnect, not a reason to spin opening empty cursors.
                    resynchronize = true;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    logger.DatabaseWatchCancelled();
                    return;
                }
                catch (Exception ex)
                {
                    if (HasInvalidResumeToken(ex))
                    {
                        resumeToken = null;
                        resynchronize = true;
                        logger.DatabaseWatchResumeTokenReset(databaseName);
                    }

                    // Without a token, reopening cannot replay writes committed while disconnected.
                    resynchronize |= resumeToken is null;
                    logger.UnexpectedError(ex);
                }
                finally
                {
                    lock (_openGate)
                    {
                        if (_opened.Task.IsCompleted)
                        {
                            _opened = new(TaskCreationOptions.RunContinuationsAsynchronously);
                        }
                    }
                }

                logger.DatabaseWatchReconnecting(databaseName, delay.TotalSeconds);
                await Task.Delay(delay, token);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.DatabaseWatchCancelled();
        }
        finally
        {
            lock (_openGate)
            {
                _opened.TrySetCanceled(token);
            }
            Changes.OnCompleted();
        }
    }

    void Publish(ChangeStreamDocument<BsonDocument> change)
    {
        try
        {
            Changes.OnNext(change);
        }
        catch (Exception ex)
        {
            logger.UnexpectedError(ex);
        }
    }
}
