// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.when_deserializing_a_read_model;

/// <summary>
/// The shape Chronicle's read model sink writes for a child collection that has never had an element — no field at all.
/// </summary>
public class without_the_collection_elements : a_read_model_document
{
    ReadModelWithEveryCollectionShape _result;

    void Establish()
    {
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Children)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.OptionalChildren)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildList)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.OrderedChildren)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildCollection)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildArray)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Tags)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.TagSet)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildrenByName)));
        _document.Remove(ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Label)));
    }

    void Because() => _result = BsonSerializer.Deserialize<ReadModelWithEveryCollectionShape>(_document);

    [Fact] void should_materialize_the_enumerable_as_empty() => _result.Children.ShouldBeEmpty();
    [Fact] void should_materialize_the_list_as_empty() => _result.ChildList.ShouldBeEmpty();
    [Fact] void should_materialize_the_read_only_list_as_empty() => _result.OrderedChildren.ShouldBeEmpty();
    [Fact] void should_materialize_the_collection_as_empty() => _result.ChildCollection.ShouldBeEmpty();
    [Fact] void should_materialize_the_array_as_empty() => _result.ChildArray.ShouldBeEmpty();
    [Fact] void should_materialize_the_hash_set_as_empty() => _result.Tags.ShouldBeEmpty();
    [Fact] void should_materialize_the_set_as_empty() => _result.TagSet.ShouldBeEmpty();
    [Fact] void should_leave_the_nullable_collection_null() => _result.OptionalChildren.ShouldBeNull();
    [Fact] void should_leave_the_dictionary_null() => _result.ChildrenByName.ShouldBeNull();
    [Fact] void should_leave_the_string_null() => _result.Label.ShouldBeNull();
}
