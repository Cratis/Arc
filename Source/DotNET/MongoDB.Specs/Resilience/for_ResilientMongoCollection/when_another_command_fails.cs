// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_ResilientMongoCollection;

public class when_another_command_fails : given.a_collection_the_server_reports_as_missing
{
    MongoCommandException _failure;
    Exception _result;

    void Establish()
    {
        _failure = CommandFailure("not authorized");
        _inner.CountDocumentsAsync(Arg.Any<FilterDefinition<string>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<long>(_failure));
    }

    async Task Because() => _result = await Catch.Exception(() => _collection.CountDocumentsAsync(Builders<string>.Filter.Empty, null!, CancellationToken.None));

    [Fact] void should_fail_with_it() => _result.ShouldEqual(_failure);
}
