// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention;

/// <summary>
/// The convention only adjusts the read direction. Writing has to stay byte-for-byte what it was, or every document
/// already in a collection would start disagreeing with the ones written after the upgrade.
/// </summary>
public class when_serializing_a_read_model : a_registered_convention_pack
{
    BsonDocument _result;

    void Because() => _result = new ReadModelWithEveryCollectionShape(
        "p1",
        [new Child("enumerable")],
        null,
        [],
        [],
        [],
        [],
        [],
        new HashSet<string>(),
        new Dictionary<string, Child>(),
        "a label").ToBsonDocument();

    [Fact] void should_write_a_populated_collection_as_an_array() => _result[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Children))].AsBsonArray.Count.ShouldEqual(1);
    [Fact] void should_write_an_empty_collection_as_an_empty_array() => _result[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.ChildList))].AsBsonArray.ShouldBeEmpty();
    [Fact] void should_write_a_null_collection_as_null() => _result[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.OptionalChildren))].IsBsonNull.ShouldBeTrue();
    [Fact] void should_write_the_string_unchanged() => _result[ElementNameFor(nameof(ReadModelWithEveryCollectionShape.Label))].AsString.ShouldEqual("a label");
}
