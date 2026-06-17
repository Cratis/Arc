// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

public class when_getting_actual_type_for_a_type_without_derivatives : given.a_derived_type_discriminator_convention
{
    Type _result;

    void Establish() => _derivedTypes.HasDerivatives(typeof(BaseType)).Returns(false);

    void Because()
    {
        var document = new BsonDocument { { DerivedTypeDiscriminatorConvention.PropertyName, _derivedTypeIdentifier } };
        using var reader = new BsonDocumentReader(document);
        _result = _convention.GetActualType(reader, typeof(BaseType));
    }

    [Fact] void should_return_the_nominal_type() => _result.ShouldEqual(typeof(BaseType));
    [Fact] void should_not_resolve_a_derived_type() => _derivedTypes.DidNotReceive().GetDerivedTypeFor(Arg.Any<Type>(), Arg.Any<DerivedTypeId>());
}
