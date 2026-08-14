// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.when_deserializing_a_read_model;

/// <summary>
/// The per-member escape hatch: a member that states its own default has said what it wants, and the convention leaves
/// it alone for an absent element and a stored null alike.
/// </summary>
public class with_a_member_declaring_its_own_default : a_registered_convention_pack
{
    BsonDocument _absent;
    BsonDocument _explicitlyNull;
    ReadModelWithOwnDefault _fromAbsent;
    ReadModelWithOwnDefault _fromExplicitNull;

    void Establish()
    {
        var elementName = BsonClassMap
            .LookupClassMap(typeof(ReadModelWithOwnDefault))
            .GetMemberMap(nameof(ReadModelWithOwnDefault.Children))
            .ElementName;

        _absent = new ReadModelWithOwnDefault("p1", [new Child("child")]).ToBsonDocument();
        _absent.Remove(elementName);

        _explicitlyNull = new ReadModelWithOwnDefault("p1", [new Child("child")]).ToBsonDocument();
        _explicitlyNull[elementName] = BsonNull.Value;
    }

    void Because()
    {
        _fromAbsent = BsonSerializer.Deserialize<ReadModelWithOwnDefault>(_absent);
        _fromExplicitNull = BsonSerializer.Deserialize<ReadModelWithOwnDefault>(_explicitlyNull);
    }

    [Fact] void should_leave_an_absent_element_null() => _fromAbsent.Children.ShouldBeNull();
    [Fact] void should_leave_a_stored_null_null() => _fromExplicitNull.Children.ShouldBeNull();
}
