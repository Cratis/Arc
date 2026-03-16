// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

public class when_getting_actual_type : given.a_derived_type_discriminator_convention
{
    Type _result;

    void Establish()
    {
        _derivedTypes
            .GetDerivedTypeFor(typeof(BaseType), new DerivedTypeId(_derivedTypeIdentifier))
            .Returns(typeof(DerivedType));
    }

    void Because()
    {
        var document = new BsonDocument { { DerivedTypeDiscriminatorConvention.PropertyName, _derivedTypeIdentifier.ToString() } };
        using var reader = new BsonDocumentReader(document);
        _result = _convention.GetActualType(reader, typeof(BaseType));
    }

    [Fact] void should_return_the_derived_type() => _result.ShouldEqual(typeof(DerivedType));
    [Fact] void should_call_get_derived_type_for_with_correct_arguments() => _derivedTypes.Received(1).GetDerivedTypeFor(typeof(BaseType), new DerivedTypeId(_derivedTypeIdentifier));
}
