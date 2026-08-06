// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention;

/// <summary>
/// The scoping to <c>[ReadModel]</c> is what keeps this from redefining what null means for every BSON type in the
/// process, so a type without the attribute must come back exactly as the driver would have left it.
/// </summary>
public class when_deserializing_a_type_that_is_not_a_read_model : a_registered_convention_pack
{
    BsonDocument _document;
    NotAReadModel _result;

    void Establish()
    {
        _document = new NotAReadModel("p1", [new Child("child")]).ToBsonDocument();
        _document.Remove(
            BsonClassMap
                .LookupClassMap(typeof(NotAReadModel))
                .GetMemberMap(nameof(NotAReadModel.Children))
                .ElementName);
    }

    void Because() => _result = BsonSerializer.Deserialize<NotAReadModel>(_document);

    [Fact] void should_leave_the_collection_null() => _result.Children.ShouldBeNull();
}
