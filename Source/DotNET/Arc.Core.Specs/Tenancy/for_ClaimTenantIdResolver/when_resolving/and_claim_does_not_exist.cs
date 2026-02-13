// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_ClaimTenantIdResolver.when_resolving;

public class and_claim_does_not_exist : given.a_claim_tenant_id_resolver
{
    string _result;

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_empty_string() => _result.ShouldEqual(string.Empty);
}
