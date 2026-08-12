// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

[Command]
public record AuthorizeWithCapturedScope(
    EventSourceId FirstTarget,
    EventSourceId SecondTarget,
    EventSourceId InterferenceSource,
    EventSourceId AuthorityScopeLabel,
    bool IncludeExactScope)
{
    public async Task<EventsWithConcurrencyScopes> Handle(IEventLog eventLog)
    {
        var authorityEventType = typeof(AuthorityRevisionAdvanced).GetEventType();
        var expectedAuthorityRevision = await eventLog.GetTailSequenceNumber(filterEventTypes: [authorityEventType]);

        await eventLog.Append(InterferenceSource, new AuthorityRevisionAdvanced());

        KeyValuePair<EventSourceId, ConcurrencyScope>[] concurrencyScopes = IncludeExactScope
            ? [new(AuthorityScopeLabel, new ConcurrencyScope(expectedAuthorityRevision, EventTypes: [authorityEventType]))]
            : [];

        return new EventsWithConcurrencyScopes(
            [
                new(FirstTarget, new FirstScopedDecisionRecorded()),
                new(SecondTarget, new SecondScopedDecisionRecorded())
            ],
            concurrencyScopes);
    }
}

[Command]
public record StartPartnerOnboardingWithScopedBatch(EventSourceId PartnerId, EventSourceId InvitationId, string OrganizationNumber)
{
    public EventsWithConcurrencyScopes Handle() => new(
        [
            new(PartnerId, new PartnerOnboardingStarted(OrganizationNumber)),
            new(InvitationId, new PartnerAdminInvited(PartnerId))
        ],
        []);
}

[EventType("cd182baa-c110-4639-a3aa-1d486e2f5fa1")]
public record AuthorityRevisionAdvanced;

[EventType("735c51da-06d9-4c3d-be9c-94933dcf11ad")]
public record FirstScopedDecisionRecorded;

[EventType("41e94332-c7e4-4657-b1b0-4ce3cc04e413")]
public record SecondScopedDecisionRecorded;
