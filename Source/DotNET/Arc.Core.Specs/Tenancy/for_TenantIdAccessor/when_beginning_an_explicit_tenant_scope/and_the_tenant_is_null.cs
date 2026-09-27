// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_the_tenant_is_null : given.a_tenant_id_accessor
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _accessor.Begin(null!));

    [Fact] void should_reject_the_tenant() => _error.ShouldBeOfExactType<ArgumentNullException>();
}
