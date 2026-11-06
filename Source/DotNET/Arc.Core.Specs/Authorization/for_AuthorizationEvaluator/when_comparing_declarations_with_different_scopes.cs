// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_comparing_declarations_with_different_scopes : Specification
{
    [Fact] void should_deny_a_changed_scope() =>
        AuthorizationEvaluator.SameDeclaration(Declared("a"), Declared("b")).ShouldBeFalse();

    [Fact] void should_deny_a_removed_scope() =>
        AuthorizationEvaluator.SameDeclaration(Declared("a"), Declared()).ShouldBeFalse();

    [Fact] void should_accept_equal_scopes() =>
        AuthorizationEvaluator.SameDeclaration(Declared("a", "b"), Declared("a", "b")).ShouldBeTrue();

    static AuthorizationDeclaration Declared(params string[] scopes) => new(false, true, []) { RequiredScopes = scopes };
}
