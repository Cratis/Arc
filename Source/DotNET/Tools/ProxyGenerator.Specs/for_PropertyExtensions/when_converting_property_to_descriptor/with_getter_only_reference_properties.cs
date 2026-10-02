// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions.when_converting_property_to_descriptor;

public class with_getter_only_reference_properties : Specification
{
    PropertyDescriptor _nullable;
    PropertyDescriptor _nonNullable;

    void Because()
    {
        _nullable = typeof(TypeWithNullableReferenceProperties).GetProperty(nameof(TypeWithNullableReferenceProperties.DisplayName))!.ToPropertyDescriptor();
        _nonNullable = typeof(TypeWithNullableReferenceProperties).GetProperty(nameof(TypeWithNullableReferenceProperties.NonNullableDisplayName))!.ToPropertyDescriptor();
    }

    [Fact] void should_mark_the_nullable_getter_only_property_as_nullable() => _nullable.IsNullable.ShouldBeTrue();
    [Fact] void should_mark_the_non_nullable_getter_only_property_as_non_nullable() => _nonNullable.IsNullable.ShouldBeFalse();
}
