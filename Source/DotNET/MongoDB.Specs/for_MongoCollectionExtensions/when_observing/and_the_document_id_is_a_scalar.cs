// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_the_document_id_is_a_scalar : given.an_observed_collection
{
    [Theory]
    [InlineData("guid", "insert")]
    [InlineData("guid", "update")]
    [InlineData("guid", "replace")]
    [InlineData("guid", "delete")]
    [InlineData("concept", "insert")]
    [InlineData("concept", "update")]
    [InlineData("concept", "replace")]
    [InlineData("concept", "delete")]
    [InlineData("object_id", "insert")]
    [InlineData("object_id", "update")]
    [InlineData("object_id", "replace")]
    [InlineData("object_id", "delete")]
    public Task should_preserve_the_typed_id_and_apply_the_change(string kind, string operation) => kind switch
    {
        "guid" => VerifyChange(Guid.NewGuid(), operation),
        "concept" => VerifyChange(new ObservedDocumentId(Guid.NewGuid()), operation),
        _ => VerifyChange(ObjectId.GenerateNewId(), operation)
    };

    static async Task VerifyChange<TId>(TId id, string operation)
    {
        using var observation = new given.an_observation_with_typed_ids<TId>(id, initiallyPresent: operation != "insert");
        await observation.Start();
        var emission = await observation.Change(operation);
        var expectedKind = operation switch
        {
            "insert" => CollectionChangeKind.Added,
            "delete" => CollectionChangeKind.Removed,
            _ => CollectionChangeKind.Replaced
        };
        emission.Changes!.Single().ShouldEqual(new CollectionChange(expectedKind, id!));
        if (operation == "delete")
        {
            emission.Documents.ShouldBeEmpty();
        }
        else
        {
            emission.Documents.Single().Name.ShouldEqual("After");
            emission.Documents.Single().Id.ShouldEqual(id);
        }
    }
}
