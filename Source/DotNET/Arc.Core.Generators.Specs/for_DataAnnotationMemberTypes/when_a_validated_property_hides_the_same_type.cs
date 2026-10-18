// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_a_validated_property_hides_the_same_type : given.hidden_validated_properties
{
    async Task Because() => await CompileHiding("[Required] public Text Name { get; } = new(\"base\");", "[Required] public new Text Name { get; } = new(\"value\");");

    [Fact] void should_not_register_the_type() => _registered.ShouldBeFalse();
    [Fact] void should_not_throw_during_initialization_or_validation() => _error.ShouldBeNull();
}
