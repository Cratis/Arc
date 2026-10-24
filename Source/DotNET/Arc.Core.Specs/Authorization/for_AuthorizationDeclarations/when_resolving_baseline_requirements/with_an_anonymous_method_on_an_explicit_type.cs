// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class with_an_anonymous_method_on_an_explicit_type : given.a_baseline
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(TypeRestricted).GetMethod(nameof(TypeRestricted.Public))!);

    [Fact] void should_allow_an_anonymous_method_despite_its_explicit_type_and_fallbacks() => _result.AllowsAnonymous.ShouldBeTrue();
}
