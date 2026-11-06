// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_the_tenant_changed_after_resolution : given.a_resolver
{
    async Task Because()
    {
        await _resolver.GetCurrent();
        _tenants.Current.Returns(new TenantId("other-tenant"));
        _result = await _resolver.GetCurrent();
    }
    [Fact] void should_refuse() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
