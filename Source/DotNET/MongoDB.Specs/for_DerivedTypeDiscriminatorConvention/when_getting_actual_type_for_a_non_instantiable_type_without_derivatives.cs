// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

public class when_getting_actual_type_for_a_non_instantiable_type_without_derivatives : given.a_derived_type_discriminator_convention
{
    Exception _result;

    void Establish() => _derivedTypes.HasDerivatives(typeof(AbstractBaseType)).Returns(false);

    void Because() => _result = Catch.Exception(() =>
    {
        var document = new BsonDocument { { DerivedTypeDiscriminatorConvention.PropertyName, _derivedTypeIdentifier } };
        using var reader = new BsonDocumentReader(document);
        _convention.GetActualType(reader, typeof(AbstractBaseType));
    });

    [Fact] void should_fail_loudly_instead_of_returning_a_type_that_recurses() => _result.ShouldBeOfExactType<CannotResolveConcreteDerivedType>();
}
