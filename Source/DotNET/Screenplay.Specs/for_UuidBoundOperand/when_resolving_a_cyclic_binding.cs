// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_UuidBoundOperand.given;

namespace Cratis.Arc.Screenplay.for_UuidBoundOperand;

public class when_resolving_a_cyclic_binding : Specification
{
    [Theory]
    [InlineData("second, first")]
    [InlineData("first, Owner")]
    public void should_refuse_the_cycle(string arguments) => bound_operands.Resolve(arguments).ShouldBeNull();
}
