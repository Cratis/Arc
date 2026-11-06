// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_reading_required_scopes;

public class with_a_blank_scope : given.scoped_operations
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _declarations.For(typeof(TypeWithBlankScope).GetMethod(nameof(TypeWithBlankScope.Run))!));

    [Fact] void should_reject_the_configuration() => _result.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
