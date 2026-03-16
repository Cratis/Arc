// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

public class when_getting_actual_type_without_discriminator_element : given.a_derived_type_discriminator_convention
{
    Type _result;

    void Because()
    {
        var document = new BsonDocument();
        using var reader = new BsonDocumentReader(document);
        _result = _convention.GetActualType(reader, typeof(BaseType));
    }

    [Fact] void should_return_the_nominal_type() => _result.ShouldEqual(typeof(BaseType));
}
