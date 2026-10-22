// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Runs bounded snapshot refreshes for a joined observation.
/// </summary>
internal static class MongoDBObservation
{
    /// <summary>
    /// Starts a joined observation with coalesced refresh notifications.
    /// </summary>
    /// <param name="changes">The shared database stream.</param>
    /// <param name="collections">The observed namespaces.</param>
    /// <param name="snapshot">Reads the current source documents.</param>
    /// <param name="selector">Combines a successful snapshot.</param>
    /// <param name="logger">The shared watch logger.</param>
    /// <param name="retryDelay">Optional retry scheduler for specifications.</param>
    /// <typeparam name="TSnapshot">The source snapshot.</typeparam>
    /// <typeparam name="TResult">The selected result.</typeparam>
    /// <returns>The live joined result.</returns>
    internal static ISubject<TResult> Create<TSnapshot, TResult>(
        IObservable<ChangeStreamDocument<BsonDocument>> changes,
        IReadOnlySet<CollectionNamespace> collections,
        Func<CancellationToken, Task<TSnapshot>> snapshot,
        Func<TSnapshot, TResult> selector,
        ILogger<MongoDBWatcher> logger,
        Func<TimeSpan, CancellationToken, Task>? retryDelay = null)
    {
        // One pending refresh is enough: every refresh reads a whole snapshot, not an event delta.
        var channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
        var cancellationGate = new object();
        var stopped = false;
        var cancelling = false;
        var cancellationRequested = false;
        var subjectStopped = false;
#pragma warning disable CA2000 // The background worker owns the cancellation source and subject lifetime.
        var cts = new CancellationTokenSource();
        var subject = LifetimeAwareSubject<TResult>.Create(Stop);
#pragma warning restore CA2000
        var token = cts.Token;
        retryDelay ??= Task.Delay;
        var subscription = changes.Subscribe(
            change =>
            {
                if (Affects(change, collections))
                {
                    channel.Writer.TryWrite(true);
                }
            },
            error => channel.Writer.TryComplete(error),
            () =>
            {
                channel.Writer.TryComplete();
                Cancel();
            });

        _ = Task.Run(Run);
        return subject;

        void Stop()
        {
            lock (cancellationGate)
            {
                subjectStopped = true;
            }
            Cancel();
        }

        void Cancel()
        {
            lock (cancellationGate)
            {
                if (stopped || cancellationRequested)
                {
                    return;
                }
                cancellationRequested = true;
                cancelling = true;
            }

            // Driver callbacks may complete the worker synchronously; never hold the gate while canceling.
            try
            {
                cts.Cancel();
            }
            finally
            {
                lock (cancellationGate)
                {
                    cancelling = false;
                    if (stopped)
                    {
                        cts.Dispose();
                    }
                }
            }
        }

        async Task Run()
        {
            try
            {
                if (changes is MongoDBDatabaseWatch databaseWatch)
                {
                    await databaseWatch.WhenOpen(token);
                }
                await Refresh();
                await foreach (var notification in channel.Reader.ReadAllAsync(token))
                {
                    await Refresh();
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // The last subscriber or the subject itself ended the observation.
            }
            catch (Exception ex)
            {
                lock (cancellationGate)
                {
                    if (!subjectStopped)
                    {
                        subject.OnError(ex);
                    }
                }
            }
            finally
            {
                subscription.Dispose();
                channel.Writer.TryComplete();
                lock (cancellationGate)
                {
                    if (!subjectStopped)
                    {
                        subject.OnCompleted();
                    }
                    subject.Dispose();
                    stopped = true;
                    if (!cancelling)
                    {
                        cts.Dispose();
                    }
                }
            }
        }

        async Task Refresh()
        {
            // A bounded retry burst keeps a failed read from terminating the subscription or spinning forever.
            // After exhaustion the next database notification starts a fresh burst, reading every joined source.
            for (var attempt = 0; attempt < 6; attempt++)
            {
                token.ThrowIfCancellationRequested();
                TSnapshot result;
                try
                {
                    result = await snapshot(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt == 5)
                    {
                        logger.SnapshotRetriesExhausted(ex);
                        return;
                    }
                    var delay = TimeSpan.FromSeconds(1 << attempt);
                    logger.SnapshotRetrying(ex, attempt + 1, delay.TotalSeconds);
                    await retryDelay(delay, token);
                    continue;
                }

                lock (cancellationGate)
                {
                    if (!cancellationRequested && !subjectStopped)
                    {
                        subject.OnNext(selector(result));
                    }
                }
                return;
            }
        }
    }

    /// <summary>
    /// Determines whether a change affects any observed namespace, including a rename destination.
    /// </summary>
    /// <param name="change">The change or resynchronization notification.</param>
    /// <param name="collections">The observed namespaces.</param>
    /// <returns>Whether a fresh snapshot is needed.</returns>
    internal static bool Affects(ChangeStreamDocument<BsonDocument> change, IReadOnlySet<CollectionNamespace> collections) =>
        change.OperationType is ChangeStreamOperationType.Invalidate or ChangeStreamOperationType.DropDatabase ||
        (change.CollectionNamespace is not null && collections.Contains(change.CollectionNamespace)) ||
        (change.OperationType == ChangeStreamOperationType.Rename && change.RenameTo is not null && collections.Contains(change.RenameTo));
}
