// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

/// <summary>
/// The shared stream carries every change in the database, including documents the observed type cannot read - which is
/// often why the filter excludes them. Reading one before knowing it is outside the filter would fault the observation.
/// </summary>
public class and_an_inserted_document_outside_the_filter_cannot_be_read : given.a_server_backed_observation
{
    ObservedDocument _inserted;
    Emission _afterChanges;

    void Establish() => _inserted = new(Guid.NewGuid(), "C");

    async Task Because()
    {
        await StartObserving();
        StubBelongsCheck(false);
        PushChange(UnreadableInsert());
        await WaitForBelongsChecks(1);
        StubBelongsCheck(true);
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterChanges = await NextEmission();
    }

    [Fact] void should_keep_observing() => Assert.Equal([new(CollectionChangeKind.Added, _inserted.Id)], _afterChanges.Changes);
    [Fact] void should_not_count_the_unreadable_document() => _afterChanges.TotalItems.ShouldEqual(3);

    static ChangeStreamDocument<BsonDocument> UnreadableInsert()
    {
        // A document whose name is not a string, under whatever element name the conventions give the property.
        var document = new ObservedDocument(Guid.NewGuid(), "unreadable").ToBsonDocument();
        var nameElement = document.Elements.Single(element => element.Value == "unreadable").Name;
        document[nameElement] = new BsonDocument("unexpected", 1);
        var id = document["_id"];
        return new(
            new BsonDocument
            {
                { "operationType", "insert" },
                { "ns", new BsonDocument { { "db", DatabaseName }, { "coll", CollectionName } } },
                { "documentKey", new BsonDocument("_id", id) },
                { "fullDocument", document }
            },
            BsonSerializer.LookupSerializer<BsonDocument>());
    }
}
