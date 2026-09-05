// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions.when_converting_property_to_descriptor;

public class with_non_generic_enumerable_property : Specification
{
    PropertyInfo _property = null!;
    Exception _error = null!;
    PropertyDescriptor _result = null!;

    void Establish() => _property = typeof(TypeWithNonGenericEnumerableProperty).GetProperty(nameof(TypeWithNonGenericEnumerableProperty.Items))!;

    void Because()
    {
        _error = Catch.Exception(() => _result = _property.ToPropertyDescriptor());
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_have_correct_name() => _result.Name.ShouldEqual("Items");
    [Fact] void should_not_be_enumerable() => _result.IsEnumerable.ShouldBeFalse();
}
