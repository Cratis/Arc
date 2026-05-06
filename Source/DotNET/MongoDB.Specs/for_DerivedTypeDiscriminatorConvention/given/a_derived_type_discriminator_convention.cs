// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention.given;

public class a_derived_type_discriminator_convention : Specification
{
    protected static readonly string _derivedTypeIdentifier = "cf759496-8e30-4d1c-92a5-51c6b1dca299";

    protected IDerivedTypes _derivedTypes;
    protected DerivedTypeDiscriminatorConvention _convention;

    void Establish()
    {
        _derivedTypes = Substitute.For<IDerivedTypes>();
        _convention = new DerivedTypeDiscriminatorConvention(_derivedTypes);
    }
}
