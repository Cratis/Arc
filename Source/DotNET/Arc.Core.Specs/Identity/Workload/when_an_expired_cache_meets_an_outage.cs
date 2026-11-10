// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_an_expired_cache_meets_an_outage : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _result;

    async Task Because()
    {
        var token = _fixture.Token();
        await _fixture.Verify(token);
        _fixture.Clock.Now += _fixture.Options.DeletedPodRejectionLatency;
        _fixture.Handler.Unavailable = true;
        _result = await _fixture.Verify(token);
    }

    [Fact] void should_not_use_stale_authority() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_attempt_fresh_verification() => _fixture.Handler.Reviews.ShouldEqual(2);
    void Destroy() => _fixture.Dispose();
}
