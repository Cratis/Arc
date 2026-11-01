// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class with_a_cancelled_collection_call : given.a_client_with_resilience_disabled
{
    readonly CancellationToken _token = new(canceled: true);
    Task<long> _nativeTask;
    Task<long> _result;
    Exception _error;

    void Establish()
    {
        _nativeTask = Task.FromCanceled<long>(_token);
        _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), _token).Returns(_nativeTask);
    }

    async Task Because()
    {
        _result = _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty, cancellationToken: _token);
        _error = await Catch.Exception(async () => await CompleteWithinDeadline(_result));
    }

    [Fact] void should_return_the_native_task() => _result.ShouldBeSame(_nativeTask);
    [Fact] void should_preserve_cancellation() => _result.IsCanceled.ShouldBeTrue();
    [Fact] void should_preserve_the_native_token() => ((OperationCanceledException)_error).CancellationToken.ShouldEqual(_token);
    [Fact] void should_forward_the_token_once() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), _token);
}
