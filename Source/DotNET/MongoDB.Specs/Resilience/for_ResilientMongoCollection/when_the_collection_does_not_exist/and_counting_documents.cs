// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_ResilientMongoCollection.when_the_collection_does_not_exist;

public class and_counting_documents : given.a_collection_the_server_reports_as_missing
{
    long _result;

    void Establish() =>
        _inner.CountDocumentsAsync(Arg.Any<FilterDefinition<string>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<long>(CollectionNotFound()));

    async Task Because() => _result = await _collection.CountDocumentsAsync(Builders<string>.Filter.Empty, null!, CancellationToken.None);

    [Fact] void should_count_none() => _result.ShouldEqual(0);
}
