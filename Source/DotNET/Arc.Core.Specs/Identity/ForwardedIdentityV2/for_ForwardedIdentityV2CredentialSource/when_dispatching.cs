// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_dispatching : Specification
{
    readonly List<CredentialVerification> _unsupported = [];
    readonly List<CredentialVerification> _credentialRefusals = [];
    CredentialVerification _browser;
    CredentialVerification _delegatedBrowser;
    CredentialVerification _missingPerson;
    CredentialVerification _pat;
    CredentialVerification _bearer;
    CredentialVerification _v1;
    CredentialVerification _v1Only;
    CredentialVerification _unreadable;
    CredentialVerification _tampered;
    int _personCalls;

    async Task Because()
    {
        var vector = VerificationFixture.DirectBrowser();
        var delegated = ForwardedIdentityV2Vectors.Named("credential-kind-browser-session");
        _delegatedBrowser = await VerificationFixture.Source(delegated).Verify(VerificationFixture.Presentation(delegated), CancellationToken.None);
        var persons = new PersonDirectory();
        _browser = await VerificationFixture.Source(vector, persons: persons).Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        _personCalls = persons.Calls;
        _missingPerson = await VerificationFixture.Source(vector, persons: new PersonDirectory(false)).Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        foreach (var name in new[] { "credential-kind-oauth-grant", "credential-kind-on-behalf-session", "credential-kind-machine-principal", "credential-kind-workload" })
        {
            var unsupported = ForwardedIdentityV2Vectors.Named(name);
            _unsupported.Add(await VerificationFixture.Source(unsupported).Verify(VerificationFixture.Presentation(unsupported), CancellationToken.None));
        }
        var pat = ForwardedIdentityV2Vectors.Named("credential-kind-pat");
        _pat = await VerificationFixture.Source(pat).Verify(VerificationFixture.Presentation(pat), CancellationToken.None);
        var bearer = ForwardedIdentityV2Vectors.Named("pat-bearer-caller-from-local-pat-verifier");
        _bearer = await VerificationFixture.Source(bearer).Verify(VerificationFixture.Presentation(bearer), CancellationToken.None);
        var v1 = vector with { Envelope = "v1.synthetic.signature" };
        _v1 = await VerificationFixture.Source(v1).Verify(VerificationFixture.Presentation(v1), CancellationToken.None);
        _v1Only = await VerificationFixture.Source(v1, v2Only: true).Verify(VerificationFixture.Presentation(v1), CancellationToken.None);
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(Substitute.For<IHttpRequestContext>());
        IVerifiedCredentialSource source = new ForwardedIdentityV2CredentialSource(requests, VerificationFixture.Settings(vector.Inputs), VerificationFixture.Verifier(vector), new("resource", "test"), new PersonDirectory(), NullLogger<ForwardedIdentityV2CredentialSource>.Instance);
        _unreadable = await source.Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        var tampered = VerificationFixture.Header(vector, "x-cratis-acr", "tampered", false);
        _tampered = await VerificationFixture.Source(tampered).Verify(VerificationFixture.Presentation(tampered), CancellationToken.None);
        foreach (var hostile in ForwardedIdentityV2Vectors.Fixture.Cases.Where(item => item.Expected.Reason == "credential"))
        {
            _credentialRefusals.Add(await VerificationFixture.Source(hostile).Verify(VerificationFixture.Presentation(hostile), CancellationToken.None));
        }
    }

    [Fact] void should_refuse_the_browser_vector_at_source_level_without_authoritative_delegation() => _delegatedBrowser.Refusal.ShouldEqual(CallerRefusal.UnsupportedProfile);
    [Fact] void should_not_return_facts_for_the_delegated_browser_vector() => _delegatedBrowser.Facts.ShouldBeNull();
    [Fact] void should_map_browser_account_through_the_directory() => _browser.Facts!.Person!.Value.ShouldEqual("persisted-person");
    [Fact] void should_lookup_the_person_once() => _personCalls.ShouldEqual(1);
    [Fact] void should_not_promote_the_raw_subject_into_a_person() => _browser.Facts!.Person!.Value.ShouldNotEqual(_browser.Facts.AccountKey!.Subject);
    [Fact] void should_not_fabricate_scopes() => _browser.Facts!.Scopes.ShouldBeEmpty();
    [Fact] void should_not_fabricate_permissions() => _browser.Facts!.Permissions.ShouldBeEmpty();
    [Fact] void should_copy_verified_assurance() => _browser.Facts!.Assurance.ShouldNotBeNull();
    [Fact] void should_preserve_credential_kind() => _browser.Facts!.CredentialKind.ShouldEqual(CallerCredentialKind.BrowserSession);
    [Fact] void should_refuse_an_unmapped_person() => _missingPerson.Refusal.ShouldEqual(CallerRefusal.MissingRequiredField);
    [Fact] void should_refuse_unsupported_envelope_only_profiles() => _unsupported.Select(result => result.Refusal).ShouldEqual(Enumerable.Repeat<CallerRefusal?>(CallerRefusal.UnsupportedProfile, 4));
    [Fact] void should_require_a_pat_verifier() => _pat.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_preserve_bearer_scopes_without_merging_proxy_scopes() => _bearer.Facts!.Scopes.ShouldEqual<string>("bearer-only");
    [Fact] void should_preserve_bearer_credential_id() => _bearer.Facts!.CredentialId.Value.ShouldEqual("verified-bearer-id");
    [Fact] void should_preserve_bearer_resource() => _bearer.Facts!.Resource.ShouldEqual("bearer-resource");
    [Fact] void should_leave_v1_to_its_source_by_default() => _v1.Applicable.ShouldBeFalse();
    [Fact] void should_refuse_v1_in_v2_only_mode() => _v1Only.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_require_raw_cardinality() => _unreadable.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
    [Fact] void should_exercise_all_credential_refusal_vectors_at_source_level() => _credentialRefusals.Count.ShouldEqual(ForwardedIdentityV2Vectors.Fixture.Cases.Count(item => item.Expected.Reason == "credential"));
    [Fact] void should_refuse_all_unbound_or_unverified_bearers_at_source_level() => _credentialRefusals.TrueForAll(result => result.Refusal == CallerRefusal.SourceRefused && result.Facts is null).ShouldBeTrue();
    [Fact] void should_claim_failed_v2() => _tampered.Applicable.ShouldBeTrue();
    [Fact] void should_refuse_failed_v2_without_fallback() => _tampered.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
}
