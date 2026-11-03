// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class with_an_unrelated_async_exception : given.a_client_with_resilience_disabled
{
    readonly InvalidOperationException _expected = new("unrelated failure");
    Exception _error;

    void Establish() => _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(_expected));

    async Task Because() => _error = await Catch.Exception(async () => await _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty));

    [Fact] void should_preserve_the_exception() => _error.ShouldBeSame(_expected);
    [Fact] void should_not_retry() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
