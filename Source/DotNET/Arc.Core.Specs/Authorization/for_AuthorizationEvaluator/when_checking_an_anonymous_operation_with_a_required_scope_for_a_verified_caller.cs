// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_checking_an_anonymous_operation_with_a_required_scope_for_a_verified_caller : Specification
{
    static readonly AuthorizationDeclaration _declaration = new(true, true, []) { RequiredScopes = ["content:write"] };

    [Fact] void should_refuse_a_caller_without_the_scope() =>
        AuthorizationEvaluator.Check(_declaration, verified_callers.PrincipalFor(verified_callers.Person("person"))).ShouldBeFalse();

    [Fact] void should_allow_a_caller_with_the_scope() =>
        AuthorizationEvaluator.Check(_declaration, verified_callers.PrincipalFor(verified_callers.Person("person", "content:write"))).ShouldBeTrue();

    [Fact] void should_allow_a_principal_without_a_verified_caller() =>
        AuthorizationEvaluator.Check(_declaration, null).ShouldBeTrue();
}
