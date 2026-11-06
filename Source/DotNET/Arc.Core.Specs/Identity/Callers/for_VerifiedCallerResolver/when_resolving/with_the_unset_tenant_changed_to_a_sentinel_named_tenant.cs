// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_the_unset_tenant_changed_to_a_sentinel_named_tenant : given.a_resolver
{
    CallerResolution _initial;

    void Establish()
    {
        _tenants.Current.Returns(TenantId.NotSet);
        _source.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(CredentialVerification.Verified(verified_facts.Service()));
    }
    async Task Because()
    {
        _initial = await _resolver.GetCurrent();
        _tenants.Current.Returns(new TenantId("notset"));
        _result = await _resolver.GetCurrent();
    }

    [Fact] void should_accept_the_initial_caller() => _initial.Caller.ShouldNotBeNull();
    [Fact] void should_refuse_the_tenant_change() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller_after_the_change() => _result.Caller.ShouldBeNull();
    [Fact] void should_verify_once() => _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
