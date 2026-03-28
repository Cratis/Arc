// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilder.when_joining;

public class and_no_filter_is_provided : given.a_joined_observe_builder
{
    IMongoCollection<DocumentC> _collection3;
    IMongoDBJoinedObserveBuilder<DocumentA, DocumentB, DocumentC> _result;

    void Establish()
    {
        _collection3 = Substitute.For<IMongoCollection<DocumentC>>();
        _collection3.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, "documentc"));

        _database
            .GetCollection<DocumentC>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(_collection3);
    }

    void Because() => _result = _builder.Join<DocumentC>();

    [Fact] void should_return_a_three_collection_joined_observe_builder() =>
        _result.ShouldBeOfExactType<MongoDBJoinedObserveBuilder<DocumentA, DocumentB, DocumentC>>();
}
