// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_verification_is_cancelled : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _cancelled;
    CredentialVerification _next;

    async Task Because()
    {
        _cancelled = await _fixture.Verify(cancellation: new CancellationToken(true));
        _next = await _fixture.Verify();
    }

    [Fact] void should_refuse_without_partial_authority() => _cancelled.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_not_cache_the_cancellation() => _next.Facts.ShouldNotBeNull();
    [Fact] void should_review_only_the_uncancelled_request() => _fixture.Handler.Reviews.ShouldEqual(1);
    void Destroy() => _fixture.Dispose();
}
