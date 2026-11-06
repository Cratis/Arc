// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_a_browser_session_with_an_invalid_scope_ceiling : given.a_browser_session_resolution
{
    readonly List<CallerResolution> _results = [];
    readonly PersonDirectory _persons = new();
    async Task Because()
    {
        foreach (var scope in new[] { "example:read  example:write", "example:read example:read", "example:\"x", "" })
        {
            var vector = VerificationFixture.Header(VerificationFixture.DirectBrowser(), "x-cratis-token-scope", scope);
            _results.Add(await Resolve(vector, new("tenant-test"), _persons));
        }
    }
    [Fact] void should_check_every_hostile_ceiling() => _results.Count.ShouldEqual(4);
    [Fact] void should_refuse() => _results.TrueForAll(result => result.Refusal == CallerRefusal.SourceRefused).ShouldBeTrue();
    [Fact] void should_have_no_caller() => _results.TrueForAll(result => result.Caller is null).ShouldBeTrue();
    [Fact] void should_refuse_before_person_lookup() => _persons.Calls.ShouldEqual(0);
}
