// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_a_contradictory_declaration;

/// <summary>
/// The very same declaration the previous spec rejects is admitted anonymously when
/// <see cref="AspNetAnonymousEvaluator"/> is asked first, because it reads the same attributes and simply prefers
/// anonymous instead of refusing to guess. Discovery order is not a specified contract, and these two outcomes do
/// not merely differ - one stops the request and the other opens it.
/// </summary>
/// <remarks>
/// Pinned, not endorsed - see https://github.com/Cratis/Arc/issues/2714.
/// </remarks>
public class with_arc_attributes_and_the_other_evaluator_first : given.both_anonymous_evaluators
{
    bool _isAuthorized;

    void Because() => _isAuthorized =
        EvaluatorWith(new AspNetAnonymousEvaluator(), new AnonymousEvaluator()).IsAuthorized(typeof(WithArcAttributes));

    [Fact] void should_silently_resolve_the_contradiction_toward_anonymous() => _isAuthorized.ShouldBeTrue();
}
