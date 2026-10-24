// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_resolving_baseline_requirements;

public class without_an_explicit_method_declaration : given.a_baseline
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(Plain).GetMethod(nameof(Plain.Run))!);

    [Fact] void should_require_all_type_and_method_fallback_requirements() => _result.Requirements.Count.ShouldEqual(2);
}
