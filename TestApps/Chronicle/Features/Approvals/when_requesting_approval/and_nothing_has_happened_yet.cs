// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Xunit;

namespace Chronicle.Features.Approvals.when_requesting_approval;

/// <summary>
/// The scenario a slice is specified by, in the shape the Screenplay generator reads one in: a scenario held on the
/// specification, a command issued through it, and assertions naming what followed.
/// </summary>
public class and_nothing_has_happened_yet
{
    readonly CommandScenario<RequestApproval> _scenario = new();
    readonly EventSourceId _approvalId = EventSourceId.New();
    CommandResult _result = null!;

    async Task Because() => _result = await _scenario.Execute(new RequestApproval(_approvalId, "Jane Austen"));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_asked_for_the_approval() => await _scenario.ShouldHaveAppendedEvent<RequestApproval, ApprovalRequested>(_approvalId);
}
