// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_a_browser_session_with_disagreeing_tenant_aliases : given.a_browser_session_resolution
{
    readonly List<CallerResolution> _results = [];
    readonly PersonDirectory _persons = new();
    async Task Because()
    {
        var vector = VerificationFixture.DirectBrowser();
        foreach (var name in new[] { "tenant-id", vector.Inputs.TenantHeader })
        {
            _results.Add(await Resolve(VerificationFixture.Header(vector, name, "other-tenant"), new("tenant-test"), _persons));
        }
    }
    [Fact] void should_check_both_aliases() => _results.Count.ShouldEqual(2);
    [Fact] void should_refuse_disagreement() => _results.TrueForAll(result => result.Refusal == CallerRefusal.TenantMismatch).ShouldBeTrue();
    [Fact] void should_have_no_caller() => _results.TrueForAll(result => result.Caller is null).ShouldBeTrue();
    [Fact] void should_refuse_before_person_lookup() => _persons.Calls.ShouldEqual(0);
}
