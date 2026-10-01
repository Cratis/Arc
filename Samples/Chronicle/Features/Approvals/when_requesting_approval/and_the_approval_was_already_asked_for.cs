// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Xunit;

namespace Chronicle.Features.Approvals.when_requesting_approval;

/// <summary>
/// The same scenario starting from a world that already held events, which is what a <c>given</c> is recovered from.
/// </summary>
public class and_the_approval_was_already_asked_for
{
    readonly CommandScenario<RequestApproval> _scenario = new();
    readonly EventSourceId _approvalId = EventSourceId.New();
    CommandResult _result = null!;

    void Establish() =>
        _scenario.Given.ForEventSource(_approvalId).Events(new ApprovalRequested("Jane Austen", []));

    async Task Because() => _result = await _scenario.Execute(new RequestApproval(_approvalId, "Charlotte Bronte"));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_asked_for_the_approval_again() => await _scenario.ShouldHaveAppendedEvent<RequestApproval, ApprovalRequested>(_approvalId);
}
