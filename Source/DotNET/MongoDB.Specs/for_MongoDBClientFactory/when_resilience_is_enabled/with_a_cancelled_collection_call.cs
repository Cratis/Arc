// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_cancelled_collection_call : given.a_client_with_resilience_enabled
{
    readonly CancellationToken _token = new(canceled: true);
    Task<long> _cancelled;
    Exception _error;
    long _nextResult;

    void Establish() => _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<CancellationToken>().IsCancellationRequested ? Task.FromCanceled<long>(_token) : Task.FromResult(23L));

    async Task Because()
    {
        _cancelled = _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty, cancellationToken: _token);
        _error = await Catch.Exception(async () => await _cancelled.WaitAsync(TimeSpan.FromSeconds(5)));
        _nextResult = await _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_preserve_cancellation() => _cancelled.IsCanceled.ShouldBeTrue();
    [Fact] void should_complete_with_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_allow_a_subsequent_call() => _nextResult.ShouldEqual(23L);
    [Fact] void should_not_retry() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), CancellationToken.None);
}
