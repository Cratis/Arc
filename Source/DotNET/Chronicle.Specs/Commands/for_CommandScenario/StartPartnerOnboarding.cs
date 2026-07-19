// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

[Command]
public record StartPartnerOnboarding(EventSourceId EventSourceId, string OrganizationNumber)
{
    public PartnerOnboardingStarted Handle() => new(OrganizationNumber);
}

[Command]
public record StartPartnerOnboardingDirect(EventSourceId EventSourceId, string OrganizationNumber)
{
    public async Task Handle(IEventLog eventLog) =>
        await eventLog.Append(EventSourceId, new PartnerOnboardingStarted(OrganizationNumber));
}

[Command]
public record StartPartnerOnboardingWithInvite(EventSourceId EventSourceId, EventSourceId InvitationId, string OrganizationNumber)
{
    public IEnumerable<EventForEventSourceId> Handle() =>
    [
        new(EventSourceId, new PartnerOnboardingStarted(OrganizationNumber)),
        new(InvitationId, new PartnerAdminInvited(EventSourceId))
    ];
}

[Command]
public record AppendInTransactionThenFail(EventSourceId EventSourceId)
{
    public async Task Handle(IEventLog eventLog)
    {
        await eventLog.Append(EventSourceId, new PartnerAdminInvited(EventSourceId));
        throw new DeliberateOnboardingFailure();
    }
}

[Command]
public record AppendOutsideTransactionThenFail(EventSourceId EventSourceId)
{
    public async Task Handle(IEventStore eventStore)
    {
        await eventStore.EventLog.Append(EventSourceId, new PartnerAdminInvited(EventSourceId));
        throw new DeliberateOnboardingFailure();
    }
}

[Command]
public record ExecuteNestedThenFail(EventSourceId EventSourceId, EventSourceId NestedEventSourceId)
{
    public async Task Handle(IEventLog eventLog, ICommandPipeline commandPipeline)
    {
        await eventLog.Append(EventSourceId, new PartnerAdminInvited(EventSourceId));
        await commandPipeline.Execute(new AppendInNestedCommand(NestedEventSourceId));
        throw new DeliberateOnboardingFailure();
    }
}

[Command]
public record AppendInNestedCommand(EventSourceId EventSourceId)
{
    public async Task Handle(IEventLog eventLog) =>
        await eventLog.Append(EventSourceId, new PartnerAdminInvited(EventSourceId));
}

/// <summary>
/// The exception that is thrown deliberately to fail a command after it has appended outside the transaction.
/// </summary>
public class DeliberateOnboardingFailure() : Exception("Deliberate failure for verifying the non-transactional opt-out.");

[EventType("e7c9a1b2-3d4f-4a5b-8c6d-7e8f9a0b1c2d")]
public record PartnerOnboardingStarted([property: Unique("UniqueOrganizationNumber", "Organization number must be unique")] string OrganizationNumber);

[EventType("a1b2c3d4-5e6f-4a7b-8c9d-0e1f2a3b4c5d")]
public record PartnerAdminInvited(EventSourceId PartnerId);
