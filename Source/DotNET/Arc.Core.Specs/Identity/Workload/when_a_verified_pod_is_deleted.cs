// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_a_verified_pod_is_deleted : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _cached;
    CredentialVerification _expired;

    async Task Because()
    {
        var token = _fixture.Token();
        await _fixture.Verify(token);
        _fixture.Handler.Pod["metadata"]!["deletionTimestamp"] = "2026-01-01T00:00:00Z";
        _fixture.Clock.Now += TimeSpan.FromSeconds(29);
        _cached = await _fixture.Verify(token);
        _fixture.Clock.Now += TimeSpan.FromSeconds(1);
        _expired = await _fixture.Verify(token);
    }

    [Fact] void should_allow_only_the_bounded_positive_cache() => _cached.Facts.ShouldNotBeNull();
    [Fact] void should_reject_at_the_declared_deletion_latency() => _expired.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_not_extend_the_cache_on_a_hit() => _fixture.Handler.Reviews.ShouldEqual(2);
    [Fact] void should_recheck_the_pod_after_the_bound() => _fixture.Handler.Pods.ShouldEqual(2);
    void Destroy() => _fixture.Dispose();
}
