// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_routing;

public class with_collection_lifecycle_changes : Specification
{
    readonly HashSet<CollectionNamespace> _collections = [new("testdb", "items")];
    bool[] _affected;
    bool _unrelated;
    bool _otherDatabase;

    void Because()
    {
        _affected = new[] { "drop", "replace", "delete", "rename" }
            .Select(operation => MongoDBObservation.Affects(Change(operation, "testdb", "items"), _collections)).ToArray();
        _unrelated = MongoDBObservation.Affects(Change("drop", "testdb", "other"), _collections);
        _otherDatabase = MongoDBObservation.Affects(Change("replace", "otherdb", "items"), _collections);
    }

    [Fact] void should_refresh_for_each_affected_collection_operation() => _affected.ShouldEachConformTo(affected => affected);
    [Fact] void should_ignore_other_collections() => _unrelated.ShouldBeFalse();
    [Fact] void should_not_confuse_matching_collection_names_in_other_databases() => _otherDatabase.ShouldBeFalse();

    static ChangeStreamDocument<BsonDocument> Change(string operation, string database, string collection) => new(
        new BsonDocument
        {
            { "operationType", operation },
            { "ns", new BsonDocument { { "db", database }, { "coll", collection } } }
        },
        BsonDocumentSerializer.Instance);
}
