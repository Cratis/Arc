// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention.given;

public class a_derived_type_discriminator_convention : Specification
{
    protected static readonly string _derivedTypeIdentifier = "b91c21cd-56db-4edc-805b-b1d1ff9aa772";

    protected IDerivedTypes _derivedTypes;
    protected DerivedTypeDiscriminatorConvention _convention;

    void Establish()
    {
        _derivedTypes = Substitute.For<IDerivedTypes>();
        _convention = new DerivedTypeDiscriminatorConvention(_derivedTypes);
    }
}
