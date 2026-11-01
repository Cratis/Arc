// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class cancelled_before_the_call_on_a_void_operation : given.a_client_with_resilience_enabled
{
    readonly CancellationToken _token = new(canceled: true);
    Task _result;
    Exception _error;

    async Task Because()
    {
        _result = _collection.InsertOneAsync(new given.Document(Guid.NewGuid()), cancellationToken: _token);
        _error = await Catch.Exception(async () => await CompleteWithinDeadline(_result));
    }

    [Fact] void should_complete_as_cancelled() => _result.IsCanceled.ShouldBeTrue();
    [Fact] void should_surface_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_call_the_driver() => _nativeCollection.DidNotReceive().InsertOneAsync(Arg.Any<given.Document>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
}
