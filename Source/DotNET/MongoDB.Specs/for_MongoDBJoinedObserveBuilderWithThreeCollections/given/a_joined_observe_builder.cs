// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilderWithThreeCollections.given;

public class a_joined_observe_builder : Specification
{
    protected const string Collection1Name = "documenta";
    protected const string Collection2Name = "documentb";
    protected const string Collection3Name = "documentc";
    protected const string DatabaseName = "testdb";

    protected IMongoCollection<DocumentA> _collection1;
    protected IMongoCollection<DocumentB> _collection2;
    protected IMongoCollection<DocumentC> _collection3;
    protected IMongoDatabase _database;
    protected Subject<ChangeStreamDocument<BsonDocument>> _databaseChanges;
    protected IMongoDBJoinedObserveBuilder<DocumentA, DocumentB, DocumentC> _builder;
    protected List<DocumentA> _docs1;
    protected List<DocumentB> _docs2;
    protected List<DocumentC> _docs3;

    void Establish()
    {
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        _docs1 = [new DocumentA("Alpha"), new DocumentA("Beta")];
        _docs2 = [new DocumentB("One"), new DocumentB("Two")];
        _docs3 = [new DocumentC("First"), new DocumentC("Second")];

        _database = Substitute.For<IMongoDatabase>();

        _collection1 = Substitute.For<IMongoCollection<DocumentA>>();
        _collection1.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, Collection1Name));
        _collection1.Database.Returns(_database);
        _collection1
            .FindAsync(
                Arg.Any<FilterDefinition<DocumentA>>(),
                Arg.Any<FindOptions<DocumentA, DocumentA>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateCursor(_docs1));

        _collection2 = Substitute.For<IMongoCollection<DocumentB>>();
        _collection2.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, Collection2Name));
        _collection2.Database.Returns(_database);
        _collection2
            .FindAsync(
                Arg.Any<FilterDefinition<DocumentB>>(),
                Arg.Any<FindOptions<DocumentB, DocumentB>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateCursor(_docs2));

        _collection3 = Substitute.For<IMongoCollection<DocumentC>>();
        _collection3.CollectionNamespace.Returns(new CollectionNamespace(DatabaseName, Collection3Name));
        _collection3.Database.Returns(_database);
        _collection3
            .FindAsync(
                Arg.Any<FilterDefinition<DocumentC>>(),
                Arg.Any<FindOptions<DocumentC, DocumentC>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateCursor(_docs3));

        _databaseChanges = new Subject<ChangeStreamDocument<BsonDocument>>();
        _builder = new MongoDBJoinedObserveBuilder<DocumentA, DocumentB, DocumentC>(
            _collection1,
            null,
            _collection2,
            null,
            _collection3,
            null,
            _databaseChanges);
    }

    protected static ChangeStreamDocument<BsonDocument> CreateChangeForCollection(string databaseName, string collectionName)
    {
        var backingDoc = new BsonDocument
        {
            { "operationType", "insert" },
            { "ns", new BsonDocument { { "db", databaseName }, { "coll", collectionName } } },
            { "documentKey", new BsonDocument { { "_id", ObjectId.GenerateNewId() } } },
            { "fullDocument", new BsonDocument() }
        };
        return new ChangeStreamDocument<BsonDocument>(backingDoc, new BsonDocumentSerializer());
    }

    static Task<IAsyncCursor<T>> CreateCursor<T>(IEnumerable<T> documents)
    {
        var list = documents.ToList();
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.Current.Returns(list);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return Task.FromResult(cursor);
    }
}
