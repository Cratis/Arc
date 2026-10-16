// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_authorizing_a_type_without_a_resource : given.a_named_policy_evaluation
{
    bool _result;

    void Establish() => _legacyEvaluator.IsAuthorized(typeof(UnboundCommand)).Returns(true);

    async Task Because() => _result = await _evaluation.IsAuthorizedForType(new TypeAuthorizationCheck(typeof(UnboundCommand), _services, _legacyEvaluator), CancellationToken.None);

    [Fact] void should_allow_the_type() => _result.ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_verdict() => _legacyEvaluator.Received(1).IsAuthorized(typeof(UnboundCommand));

    public record UnboundCommand;
}
