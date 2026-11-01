// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class cancelled_while_waiting_for_a_connection_slot_on_a_void_operation : given.a_client_with_resilience_enabled
{
    readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _cancellation = new();
    readonly given.Document _document = new(Guid.NewGuid());
    Task[] _pending;
    Task _queued;
    Exception _error;

    void Establish() => _nativeCollection.InsertOneAsync(_document, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(_completion.Task);

    async Task Because()
    {
        _pending = [_collection.InsertOneAsync(_document), _collection.InsertOneAsync(_document)];
        _queued = _collection.InsertOneAsync(_document, cancellationToken: _cancellation.Token);
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(async () => await CompleteWithinDeadline(_queued));
    }

    async Task Destroy()
    {
        _completion.TrySetResult(true);
        await CompleteWithinDeadline(Task.WhenAll(_pending));
        _cancellation.Dispose();
    }

    [Fact] void should_cancel_the_queued_call() => _queued.IsCanceled.ShouldBeTrue();
    [Fact] void should_surface_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_invoke_the_queued_call() => _nativeCollection.Received(2).InsertOneAsync(_document, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
}
