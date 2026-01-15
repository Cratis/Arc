// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions.when_checking_is_optional;

public class with_nullable_object_property : Specification
{
    PropertyInfo _property;
    bool _result;

    void Establish() => _property = typeof(TypeWithNullableReferenceProperties).GetProperty(nameof(TypeWithNullableReferenceProperties.NullableObject));

    void Because() => _result = _property.IsOptional();

    [Fact] void should_be_optional() => _result.ShouldBeTrue();
}
