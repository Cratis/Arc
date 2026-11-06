// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_checking_roles_for_a_verified_caller_without_the_scope : Specification
{
    static readonly AuthorizationDeclaration _declaration = new(false, true, [new AuthorizationRequirement(["editor"])]) { RequiredScopes = ["content:write"] };

    bool _withoutScope;
    bool _withScope;
    bool _noRequirementsNoScope;

    void Because()
    {
        var withoutScope = verified_callers.PrincipalFor(verified_callers.Person("person"));
        ((ClaimsIdentity)withoutScope.Identity!).AddClaim(new Claim(ClaimTypes.Role, "editor"));
        var withScope = verified_callers.PrincipalFor(verified_callers.Person("person", "content:write"));
        ((ClaimsIdentity)withScope.Identity!).AddClaim(new Claim(ClaimTypes.Role, "editor"));
        _withoutScope = AuthorizationEvaluator.CheckRoles(_declaration, withoutScope);
        _withScope = AuthorizationEvaluator.CheckRoles(_declaration, withScope);
        _noRequirementsNoScope = AuthorizationEvaluator.CheckRoles(new(false, true, []) { RequiredScopes = ["content:write"] }, verified_callers.PrincipalFor(verified_callers.Person("person")));
    }

    [Fact] void should_refuse_even_though_the_role_is_held() => _withoutScope.ShouldBeFalse();

    [Fact] void should_allow_when_role_and_scope_are_held() => _withScope.ShouldBeTrue();

    [Fact] void should_refuse_a_scope_only_declaration_without_the_scope() => _noRequirementsNoScope.ShouldBeFalse();
}
