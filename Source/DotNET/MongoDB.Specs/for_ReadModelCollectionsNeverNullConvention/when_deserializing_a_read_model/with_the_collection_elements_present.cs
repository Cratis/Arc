// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.when_deserializing_a_read_model;

/// <summary>
/// A stored value must survive the convention untouched — the guarantee is about absence, not about the content.
/// </summary>
public class with_the_collection_elements_present : a_read_model_document
{
    ReadModelWithEveryCollectionShape _result;

    void Because() => _result = BsonSerializer.Deserialize<ReadModelWithEveryCollectionShape>(_document);

    [Fact] void should_keep_the_enumerable_value() => _result.Children.Single().Name.ShouldEqual("enumerable");
    [Fact] void should_keep_the_list_value() => _result.ChildList.Single().Name.ShouldEqual("list");
    [Fact] void should_keep_the_read_only_list_value() => _result.OrderedChildren.Single().Name.ShouldEqual("ordered");
    [Fact] void should_keep_the_collection_value() => _result.ChildCollection.Single().Name.ShouldEqual("collection");
    [Fact] void should_keep_the_array_value() => _result.ChildArray.Single().Name.ShouldEqual("array");
    [Fact] void should_keep_the_hash_set_value() => _result.Tags.Single().ShouldEqual("tag");
    [Fact] void should_keep_the_set_value() => _result.TagSet.Single().ShouldEqual("set");
    [Fact] void should_keep_the_nullable_collection_value() => _result.OptionalChildren!.Single().Name.ShouldEqual("optional");
    [Fact] void should_keep_the_dictionary_value() => _result.ChildrenByName["key"].Name.ShouldEqual("mapped");
    [Fact] void should_keep_the_string_value() => _result.Label.ShouldEqual("a label");
}
