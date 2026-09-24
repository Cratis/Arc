// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_legacy_evaluator_denies : given.a_named_policy_evaluation
{
    bool _allowed;

    void Establish() => _legacyEvaluator.IsAuthorized(typeof(ProtectedCommand)).Returns(false);

    async Task Because() => _allowed = await Authorize();

    [Fact] void should_keep_its_denial_authoritative() => _allowed.ShouldBeFalse();
}
