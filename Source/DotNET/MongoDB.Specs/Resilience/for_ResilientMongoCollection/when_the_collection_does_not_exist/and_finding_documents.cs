// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_ResilientMongoCollection.when_the_collection_does_not_exist;

public class and_finding_documents : given.a_collection_the_server_reports_as_missing
{
    IAsyncCursor<string> _result;

    void Establish() =>
        _inner.FindAsync(Arg.Any<FilterDefinition<string>>(), Arg.Any<FindOptions<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IAsyncCursor<string>>(CollectionNotFound()));

    async Task Because() => _result = await _collection.FindAsync(Builders<string>.Filter.Empty, (FindOptions<string, string>)null!, CancellationToken.None);

    [Fact] async Task should_find_nothing() => (await _result.MoveNextAsync(CancellationToken.None)).ShouldBeFalse();
}
