// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention.given;

public class a_derived_type_discriminator_convention : Specification
{
    protected static readonly Guid _derivedTypeIdentifier = new(0xb91c21cd, 0x56db, 0x4edc, 0x80, 0x5b, 0xb1, 0xd1, 0xff, 0x9a, 0xa7, 0x72);

    protected IDerivedTypes _derivedTypes;
    protected DerivedTypeDiscriminatorConvention _convention;

    void Establish()
    {
        _derivedTypes = Substitute.For<IDerivedTypes>();
        _convention = new DerivedTypeDiscriminatorConvention(_derivedTypes);
    }
}
