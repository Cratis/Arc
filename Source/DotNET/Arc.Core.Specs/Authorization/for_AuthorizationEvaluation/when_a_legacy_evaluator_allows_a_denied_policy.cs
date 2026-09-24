// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_legacy_evaluator_allows_a_denied_policy : given.a_named_policy_evaluation
{
    bool _allowed;

    void Establish() => _legacyEvaluator.IsAuthorized(typeof(ProtectedDeniedCommand)).Returns(true);

    async Task Because() => _allowed = await AuthorizeDenied();

    [Fact] void should_not_bypass_the_policy() => _allowed.ShouldBeFalse();
}
