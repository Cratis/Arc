// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

/// <summary>
/// A server-backed observation whose first query after the initial read is held until the spec lets it go, so changes
/// arrive while the observation is still applying an earlier one - as they do when the server is slow to answer.
/// </summary>
public class a_busy_observation : a_server_backed_observation
{
    protected int _belongsChecks;
    protected int _counts;
    protected int _reads;

    readonly TaskCompletionSource _busy = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _held;

    void Establish()
    {
        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref _belongsChecks);
                await HoldTheFirst();
                return CreateCursor<BsonDocument>([new BsonDocument()]);
            });

        _collection
            .CountDocumentsAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                // The first count belongs to the initial read.
                if (Interlocked.Increment(ref _counts) > 1)
                {
                    await HoldTheFirst();
                }

                return (long)_documents.Count(_matches);
            });

        _collection
            .When(_ => _.FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, ObservedDocument>>(),
                Arg.Any<CancellationToken>()))
            .Do(_ => Interlocked.Increment(ref _reads));
    }

    void Destroy() => _released.TrySetResult();

    /// <summary>
    /// Waits until the observation is held applying a change.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when it is.</returns>
    protected Task Busy() => _busy.Task.WaitAsync(Deadline);

    /// <summary>
    /// Lets the held change finish.
    /// </summary>
    protected void Release() => _released.TrySetResult();

    async Task HoldTheFirst()
    {
        if (Interlocked.Increment(ref _held) == 1)
        {
            _busy.TrySetResult();
            await _released.Task;
        }
    }
}
