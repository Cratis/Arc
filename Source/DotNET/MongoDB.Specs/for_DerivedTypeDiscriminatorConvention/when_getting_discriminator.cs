// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

public class when_getting_discriminator : given.a_derived_type_discriminator_convention
{
    BsonValue _result;

    void Because() => _result = _convention.GetDiscriminator(typeof(BaseType), typeof(DerivedType));

    [Fact] void should_return_the_derived_type_identifier() => _result.AsString.ShouldEqual(_derivedTypeIdentifier.ToString());
}
