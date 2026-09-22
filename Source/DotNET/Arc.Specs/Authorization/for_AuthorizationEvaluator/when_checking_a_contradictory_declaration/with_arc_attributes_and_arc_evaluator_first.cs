// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_a_contradictory_declaration;

public class with_arc_attributes_and_arc_evaluator_first : given.both_anonymous_evaluators
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
        EvaluatorWith(new AnonymousEvaluator(), new AspNetAnonymousEvaluator()).IsAuthorized(typeof(WithArcAttributes)));

    [Fact] void should_reject_the_contradiction() => _exception.ShouldBeOfExactType<AmbiguousAuthorizationLevel>();
}
