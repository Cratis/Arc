// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_reading_required_scopes;

public class without_scopes : given.scoped_operations
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(TypeWithoutScopes).GetMethod(nameof(TypeWithoutScopes.Run))!);

    [Fact] void should_require_none() => _result.RequiredScopes.ShouldBeEmpty();
}
