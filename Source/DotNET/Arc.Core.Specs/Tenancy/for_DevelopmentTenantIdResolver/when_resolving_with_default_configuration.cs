// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_DevelopmentTenantIdResolver;

public class when_resolving_with_default_configuration : given.a_development_tenant_id_resolver
{
    string _result;

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_default_development_tenant_id() => _result.ShouldEqual("development");
}
