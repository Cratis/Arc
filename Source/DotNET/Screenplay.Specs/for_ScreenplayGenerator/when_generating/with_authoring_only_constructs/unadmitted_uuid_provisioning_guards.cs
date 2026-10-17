// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class unadmitted_uuid_provisioning_guards : an_authoring_document
{
    [Theory]
    [InlineData("grant.Instance!.Optional == Requester", "optional operands are not admitted")]
    [InlineData("(Guid)grant.Instance!.Optional == Requester", "casts that change the operand type")]
    [InlineData("grant.Instance!.MemberId == RoomId", "different Uuid concepts or identities")]
    [InlineData("grant.Instance!.MemberId < RequestedBy", "only == and != are admitted")]
    public void should_report_the_refusal_and_keep_the_guard_in_code(string comparison, string reason)
    {
        Generate(UuidComparisonSources.Provisioning(comparison));
        Result.Source.ShouldNotContain("require ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldBeTrue();
    }
}
