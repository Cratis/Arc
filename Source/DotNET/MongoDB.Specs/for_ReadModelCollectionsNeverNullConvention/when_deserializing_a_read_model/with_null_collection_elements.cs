// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.when_deserializing_a_read_model;

/// <summary>
/// The other shape a store can leave behind — the field is there and holds null. A default value alone never covers
/// this, because the driver goes through the member's serializer for an element that is present.
/// </summary>
public class with_null_collection_elements : a_read_model_document
{
    ReadModelWithEveryCollectionShape _result;

    void Establish()
    {
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Children))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.OptionalChildren))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildList))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.OrderedChildren))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildCollection))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildArray))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Tags))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.TagSet))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildrenByName))] = BsonNull.Value;
        _document[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Label))] = BsonNull.Value;
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
