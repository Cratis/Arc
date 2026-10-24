// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class with_an_explicit_method : given.a_baseline
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Restricted))!);

    [Fact]
    void should_prefer_an_explicit_method_over_its_type_and_fallbacks() =>
        _result.Requirements.Single().AnyOfRoles.Single().ShouldEqual("Method");
    [Fact] void should_not_consult_fallbacks_for_explicit_declarations() => _changing.ReadCount.ShouldEqual(0);
}
