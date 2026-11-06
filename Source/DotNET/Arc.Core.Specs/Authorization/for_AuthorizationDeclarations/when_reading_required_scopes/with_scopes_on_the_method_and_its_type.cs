// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_reading_required_scopes;

public class with_scopes_on_the_method_and_its_type : given.scoped_operations
{
    AuthorizationDeclaration _result;

    void Because() => _result = _declarations.For(typeof(TypeWithOneScope).GetMethod(nameof(TypeWithOneScope.Run))!);

    [Fact] void should_union_the_scopes_in_ordinal_order_without_duplicates() => string.Join(',', _result.RequiredScopes).ShouldEqual("a:read,b:write");
}
