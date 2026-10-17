// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_UuidBoundOperand.given;

namespace Cratis.Arc.Screenplay.for_UuidBoundOperand;

public class when_resolving_a_deep_binding_chain : Specification
{
    [Theory]
    [InlineData(63, true)]
    [InlineData(64, false)]
    public void should_bound_the_number_of_expressions_followed(int count, bool admitted)
    {
        var names = Enumerable.Range(1, count - 1).Select(index => $"value{index}").Prepend("first").ToArray();
        var parameters = string.Join(", ", names.Select(name => $"Guid {name}"));
        var arguments = string.Join(", ", names.Skip(1).Append("Owner"));
        (bound_operands.Resolve(arguments, parameters) is not null).ShouldEqual(admitted);
    }
}
