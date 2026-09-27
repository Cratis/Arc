// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_reusing_a_guest_evaluation_decision : Specification
{
    bool _same;
    bool _differentDecision;

    void Because()
    {
        var declaration = new AuthorizationDeclaration(false, true, [AuthorizationRequirement.FromAttribute(null, "Guest", null)]);
        _same = AuthorizationEvaluator.SameDeclaration(declaration, declaration, true, true);
        _differentDecision = AuthorizationEvaluator.SameDeclaration(declaration, declaration, true, false);
    }

    [Fact] void should_allow_only_the_same_plan() => _same.ShouldBeTrue();
    [Fact] void should_not_reuse_a_guest_verdict_for_a_different_registration() => _differentDecision.ShouldBeFalse();
}
