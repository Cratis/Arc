// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class calling_one_method_more_times_than_there_are_connection_slots : given.a_client_with_resilience_disabled
{
    readonly TaskCompletionSource<long> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<long>[] _calls;

    void Establish() => _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(_completion.Task);

    void Because() => _calls = [_collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty), _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty), _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty)];

    async Task Destroy()
    {
        _completion.TrySetResult(0L);
        await CompleteWithinDeadline(Task.WhenAll(_calls));
    }

    [Fact] void should_admit_all_three_calls() => _nativeCollection.Received(3).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_return_the_native_tasks() => _calls.ShouldEachConformTo(call => ReferenceEquals(call, _completion.Task));
}
