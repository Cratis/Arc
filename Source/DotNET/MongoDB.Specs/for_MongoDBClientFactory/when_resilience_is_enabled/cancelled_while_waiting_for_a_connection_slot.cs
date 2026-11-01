// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class cancelled_while_waiting_for_a_connection_slot : given.a_client_with_resilience_enabled
{
    readonly TaskCompletionSource<long> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _cancellation = new();
    Task<long>[] _pending;
    Task<long> _queued;
    Exception _error;

    void Establish() => _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(_completion.Task);

    async Task Because()
    {
        _pending = [_collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty), _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty)];
        _queued = _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty, cancellationToken: _cancellation.Token);
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(async () => await CompleteWithinDeadline(_queued));
    }

    async Task Destroy()
    {
        _completion.TrySetResult(0L);
        await CompleteWithinDeadline(Task.WhenAll(_pending));
        _cancellation.Dispose();
    }

    [Fact] void should_cancel_the_queued_call() => _queued.IsCanceled.ShouldBeTrue();
    [Fact] void should_surface_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_invoke_the_queued_call() => _nativeCollection.Received(2).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
