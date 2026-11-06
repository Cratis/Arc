// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_the_admitted_hop_certificate_changed : given.a_v2_resolver
{
    async Task Because()
    {
        _first = await _resolver.GetCurrent();
        ChangeCertificate();
        _result = await _resolver.GetCurrent();
    }

    [Fact] void should_accept_the_first_resolution() => _first.Caller.ShouldNotBeNull();
    [Fact] void should_still_admit_the_replacement_hop() => ForwardedIdentityHop.IsAdmitted(_request).ShouldBeTrue();
    [Fact] void should_refuse_reuse() => _result.Refusal.ShouldEqual(CallerRefusal.RequestChanged);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
    [Fact] void should_not_resolve_the_person_again() => _persons.Calls.ShouldEqual(1);
}
