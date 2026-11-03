// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class calling_one_method_more_times_than_there_are_connection_slots : given.a_client_with_resilience_enabled
{
    readonly TaskCompletionSource<long> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<long>[] _pending;
    Exception _error;
    long _otherMethodResult;

    void Establish()
    {
        _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(_completion.Task);
        _nativeCollection.EstimatedDocumentCountAsync(Arg.Any<EstimatedDocumentCountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(5L));
    }

    async Task Because()
    {
        _pending = [_collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty), _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty)];
        var third = _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty);
        var otherMethod = _collection.EstimatedDocumentCountAsync();
        await CompleteWithinDeadline(otherMethod);
        _otherMethodResult = await otherMethod;
        _error = await Catch.Exception(async () => await CompleteWithinDeadline(third));
    }

    async Task Destroy()
    {
        _completion.TrySetResult(0L);
        await CompleteWithinDeadline(Task.WhenAll(_pending));
    }

    [Fact] void should_only_admit_half_the_pool() => _nativeCollection.Received(2).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_timeout_the_over_limit_call() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_report_the_acquire_timeout() => _error.Message.ShouldEqual("Failed to acquire semaphore.");
    [Fact] void should_admit_another_method_on_the_same_collection() => _otherMethodResult.ShouldEqual(5L);
}
