// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class without_an_explicit_type_declaration : given.a_baseline
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(Plain));

    [Fact]
    void should_apply_the_baseline_to_a_type_without_an_explicit_declaration() =>
        _result.Requirements.Single().AnyOfRoles.Single().ShouldEqual("Baseline");
}
