// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_browser_contexts : Specification
{
    CallerResolution _direct;
    CallerResolution _delegated;
    CallerResolution _grantOnly;
    ForwardedIdentityV2Verification _verified;
    int _delegatedPersonLookups;

    async Task Because()
    {
        var delegated = ForwardedIdentityV2Vectors.Named("credential-kind-browser-session");
        _verified = await VerificationFixture.Verifier(delegated).Verify(VerificationFixture.Settings(delegated.Inputs), VerificationFixture.Request(delegated), CancellationToken.None);
        _direct = await Resolve(VerificationFixture.DirectBrowser(), new PersonDirectory());
        var persons = new PersonDirectory();
        _delegated = await Resolve(delegated, persons);
        _grantOnly = await Resolve(VerificationFixture.Header(delegated, "x-cratis-act", null), persons);
        _delegatedPersonLookups = persons.Calls;
    }

    static async Task<CallerResolution> Resolve(Vector vector, PersonDirectory persons)
    {
        var request = VerificationFixture.RawRequest(vector);
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(request);
        var tenants = Substitute.For<ITenantIdAccessor>();
        tenants.Current.Returns(new TenantId("tenant-test"));
        var source = VerificationFixture.Source(vector, persons: persons);
        var catalog = new CallerResourceCatalog(new Dictionary<string, string> { ["resource"] = vector.Inputs.Aud });
        var resolver = new VerifiedCallerResolver(requests, [source], new(), new(catalog), new("resource", "test"), tenants);
        return await resolver.GetCurrent();
    }

    [Fact] void should_preserve_verifier_level_acceptance_of_the_browser_vector() => _verified.Rejection.ShouldBeNull();
    [Fact] void should_keep_the_authenticated_actor_in_the_verified_context() => _verified.Context!.Actor.ShouldNotBeNull();
    [Fact] void should_keep_the_bounded_grant_in_the_verified_context() => _verified.Context!.GrantId.ShouldNotBeNull();
    [Fact] void should_construct_a_direct_browser_caller_with_the_real_builder() => _direct.Caller!.PrincipalKind.ShouldEqual(CallerPrincipalKind.Person);
    [Fact] void should_accept_the_direct_browser_control() => _direct.Refusal.ShouldBeNull();
    [Fact] void should_refuse_the_delegated_browser_context() => _delegated.Refusal.ShouldEqual(CallerRefusal.UnsupportedProfile);
    [Fact] void should_never_downgrade_delegation_to_an_ordinary_person() => _delegated.Caller.ShouldBeNull();
    [Fact] void should_refuse_a_bounded_grant_without_an_actor() => _grantOnly.Refusal.ShouldEqual(CallerRefusal.UnsupportedProfile);
    [Fact] void should_never_downgrade_a_bounded_grant_to_an_ordinary_person() => _grantOnly.Caller.ShouldBeNull();
    [Fact] void should_refuse_before_person_lookup() => _delegatedPersonLookups.ShouldEqual(0);
}
