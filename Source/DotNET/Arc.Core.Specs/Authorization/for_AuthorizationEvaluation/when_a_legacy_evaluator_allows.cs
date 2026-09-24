// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_legacy_evaluator_allows : given.a_named_policy_evaluation
{
    bool _allowed;

    void Establish() => _legacyEvaluator.IsAuthorized(typeof(ProtectedCommand)).Returns(true);

    async Task Because() => _allowed = await Authorize();

    [Fact] void should_allow_only_after_the_policy_succeeds() => _allowed.ShouldBeTrue();
}
