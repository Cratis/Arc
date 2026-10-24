// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class with_an_anonymous_method : given.a_baseline
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(Plain).GetMethod(nameof(Plain.Public))!);

    [Fact] void should_not_apply_a_fallback_to_an_anonymous_method() => _result.AllowsAnonymous.ShouldBeTrue();
    [Fact] void should_not_consult_fallbacks_for_explicit_declarations() => _changing.ReadCount.ShouldEqual(0);
}
