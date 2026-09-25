// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_attributes_contradict;

public class across_attribute_families : given.both_attribute_families
{
    Exception _arcFirst;
    Exception _aspNetFirst;

    void Because()
    {
        _arcFirst = Catch.Exception(() => ArcFirst().IsAuthorized(typeof(ContradictingAcrossFamilies)));
        _aspNetFirst = Catch.Exception(() => AspNetFirst().IsAuthorized(typeof(ContradictingAcrossFamilies)));
    }

    [Fact] void should_reject_it_when_arc_is_asked_first() => _arcFirst.ShouldBeOfExactType<AmbiguousAuthorizationLevel>();
    [Fact] void should_reject_it_when_aspnet_core_is_asked_first() => _aspNetFirst.ShouldBeOfExactType<AmbiguousAuthorizationLevel>();
}
