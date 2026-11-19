// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_a_cached_token_expires : Specification
{
    readonly given.workload_verification _fixture = new();
    CredentialVerification _result;

    async Task Because()
    {
        var token = _fixture.Token(10);
        await _fixture.Verify(token);
        _fixture.Clock.Now += TimeSpan.FromSeconds(10);
        _result = await _fixture.Verify(token);
    }

    [Fact] void should_not_outlive_the_credential() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_not_review_a_locally_expired_token() => _fixture.Handler.Reviews.ShouldEqual(1);
    void Destroy() => _fixture.Dispose();
}
