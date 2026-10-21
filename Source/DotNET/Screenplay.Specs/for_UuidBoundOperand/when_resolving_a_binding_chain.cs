// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_UuidBoundOperand.given;

namespace Cratis.Arc.Screenplay.for_UuidBoundOperand;

public class when_resolving_a_binding_chain : Specification
{
    [Theory]
    [InlineData("second, Owner", "Owner", false, false)]
    [InlineData("(Guid)second, (Owner)", "(Owner)", false, false)]
    [InlineData("second, (Guid)Maybe", "(Guid)Maybe", true, true)]
    public void should_follow_all_bindings_without_losing_casts_or_optionality(string arguments, string expected, bool cast, bool optional)
    {
        var operand = bound_operands.Resolve(arguments)!;
        operand.Expressions.Count.ShouldEqual(3);
        operand.Value.Expression.ToString().ShouldEqual(expected);
        operand.HasNonTransparentCast.ShouldEqual(cast);
        operand.IsOptional.ShouldEqual(optional);
        operand.Value.Expression.SyntaxTree.ShouldBeSame(operand.Value.SemanticModel.SyntaxTree);
    }
}
