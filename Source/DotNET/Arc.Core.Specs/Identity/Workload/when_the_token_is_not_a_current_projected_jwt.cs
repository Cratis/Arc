// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_the_token_is_not_a_current_projected_jwt : Specification
{
    readonly given.workload_verification _fixture = new();
    readonly List<CredentialVerification> _results = [];

    async Task Because()
    {
        foreach (var token in new[] { "legacy-secret-fixture", "header.invalid-json.signature", _fixture.Token(0), new string('a', 65_537) })
        {
            _results.Add(await _fixture.Verify(token));
        }
    }

    [Fact] void should_refuse_every_invalid_token() => _results.TrueForAll(result => result.Refusal == CallerRefusal.SourceRefused).ShouldBeTrue();
    [Fact] void should_not_supply_any_authority() => _results.TrueForAll(result => result.Facts is null).ShouldBeTrue();
    [Fact] void should_not_contact_kubernetes() => _fixture.Handler.Reviews.ShouldEqual(0);
    void Destroy() => _fixture.Dispose();
}
