// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBObserveBuilder.given;

public class an_observe_builder : Specification
{
    protected IMongoCollection<DocumentA> _collection1;
    protected IMongoCollection<DocumentB> _collection2;
    protected IMongoDatabase _database;
    protected Subject<ChangeStreamDocument<BsonDocument>> _databaseChanges;
    protected IMongoDBObserveBuilder<DocumentA> _builder;

    void Establish()
    {
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        _collection2 = Substitute.For<IMongoCollection<DocumentB>>();
        _collection2.CollectionNamespace.Returns(new CollectionNamespace("testdb", "documentb"));

        _database = Substitute.For<IMongoDatabase>();
        _database
            .GetCollection<DocumentB>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(_collection2);

        _collection1 = Substitute.For<IMongoCollection<DocumentA>>();
        _collection1.CollectionNamespace.Returns(new CollectionNamespace("testdb", "documenta"));
        _collection1.Database.Returns(_database);

        _databaseChanges = new Subject<ChangeStreamDocument<BsonDocument>>();
        _builder = new MongoDBObserveBuilder<DocumentA>(_collection1, null, _databaseChanges);
    }
}
