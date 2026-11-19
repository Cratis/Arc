// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

public class when_the_reviewer_credential_rotates : Specification
{
    readonly given.workload_verification _fixture = new();

    async Task Because()
    {
        await _fixture.Verify(_fixture.Token(300));
        _fixture.Reviewer = "rotated-reviewer-fixture";
        await _fixture.Verify(_fixture.Token(301));
    }

    [Fact] void should_reread_for_every_api_call() => _fixture.Handler.ReviewerCredentials.ShouldEqual(new[] { "reviewer-fixture", "reviewer-fixture", "rotated-reviewer-fixture", "rotated-reviewer-fixture" });
    void Destroy() => _fixture.Dispose();
}
