// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_the_receiving_target_changes : Specification
{
    readonly given.workload_verification _fixture = new();
    readonly List<CredentialVerification> _results = [];

    async Task Because()
    {
        var token = _fixture.Token();
        await _fixture.Verify(token);
        var targets = new[]
        {
            _fixture.Target with { Audience = "different-audience" },
            _fixture.Target with { Resource = "https://other.example" },
            _fixture.Target with { Environment = "other" }
        };
        foreach (var target in targets) _results.Add(await _fixture.CreateSource(target).Verify(given.workload_verification.Present(token), CancellationToken.None));
    }

    [Fact] void should_not_reuse_other_target_authority() => _results.TrueForAll(result => result.Refusal == CallerRefusal.SourceRefused).ShouldBeTrue();
    [Fact] void should_review_each_exact_target() => _fixture.Handler.Reviews.ShouldEqual(4);
    void Destroy() => _fixture.Dispose();
}
