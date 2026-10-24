// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class with_a_changed_fallback : given.a_baseline
{
    string _first;
    string _second;

    void Establish()
    {
        _first = _declarations.For(typeof(Plain)).Requirements.Single().AnyOfRoles.Single();
        _changing.Role = "Changed";
    }

    void Because() => _second = _declarations.For(typeof(Plain)).Requirements.Single().AnyOfRoles.Single();

    [Fact]
    void should_not_reuse_a_previous_fallback_result()
    {
        _first.ShouldEqual("Baseline");
        _second.ShouldEqual("Changed");
    }
}
