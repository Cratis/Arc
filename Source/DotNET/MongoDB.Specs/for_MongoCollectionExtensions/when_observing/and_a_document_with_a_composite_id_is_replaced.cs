// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_a_document_with_a_composite_id_is_replaced : given.an_observed_collection
{
    readonly CompositeDocumentId _id = new("author", 42);
    given.an_observation_with_typed_ids<CompositeDocumentId>.Emission _emission;

    async Task Because()
    {
        using var observation = new given.an_observation_with_typed_ids<CompositeDocumentId>(_id);
        await observation.Start();
        _emission = await observation.Change("replace");
    }

    [Fact] void should_emit_the_replacement_document() => _emission.Documents.Single().Name.ShouldEqual("After");
    [Fact] void should_report_the_typed_id_in_the_change() => _emission.Changes!.Single().ShouldEqual(new CollectionChange(CollectionChangeKind.Replaced, _id));
}
