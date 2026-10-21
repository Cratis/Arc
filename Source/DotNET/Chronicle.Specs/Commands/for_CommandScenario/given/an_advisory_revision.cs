// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.given;

public class an_advisory_revision : Specification
{
    protected readonly EventSourceId _source = EventSourceId.New();
    protected readonly DateTimeOffset _occurred = new(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected CommandResult _result;

    protected async Task Seed<TCommand>(CommandScenario<TCommand> scenario)
    {
        // Earlier events belong to another source, so source-local replay must not renumber the relevant event.
        for (var sequence = EventSequenceNumber.First.Value; sequence < 2; sequence++)
        {
            scenario.Given.ForEventSource(EventSourceId.New()).Events(new RevisionNoted());
        }

        (await scenario.EventLog.Append(_source, new RevisionNoted(), occurred: _occurred)).IsSuccess.ShouldBeTrue();
        (await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source)).Single().Context.SequenceNumber.ShouldEqual((EventSequenceNumber)2ul);
    }

    [FromEvent<RevisionNoted>]
    public record AdvisoryRevision(
        [property: Key] Guid Id,
        [property: SetFromContext<RevisionNoted>(nameof(EventContext.SequenceNumber))] ulong Revision,
        [property: SetFromContext<RevisionNoted>(nameof(EventContext.Occurred))] DateTimeOffset Occurred);

    [Command]
    public record ReadAdvisoryRevision(EventSourceId EventSourceId)
    {
        public EventsWithConcurrencyScopes Handle(AdvisoryRevision advisory) => new(
            [new(EventSourceId, new RevisionObserved(advisory.Revision, advisory.Occurred, true))],
            [new(EventSourceId, new ConcurrencyScope(advisory.Revision, EventSourceId: EventSourceId))]);
    }

    [Command]
    [ProtectedDecision]
    [ReadProtection(ReadProtectionMode.On)]
    public record ReadAdvisoryRevisionWithProtection(EventSourceId EventSourceId)
    {
        public EventsWithConcurrencyScopes Handle(AdvisoryRevision guarded, [Unprotected("Compare the advisory revision with the protected fold")] AdvisoryRevision advisory) => new(
            [new(EventSourceId, new RevisionObserved(advisory.Revision, advisory.Occurred, guarded == advisory))],
            [new(EventSourceId, new ConcurrencyScope(advisory.Revision, EventSourceId: EventSourceId))]);
    }

    [EventType]
    public record RevisionNoted;

    [EventType]
    public record RevisionObserved(ulong Revision, DateTimeOffset Occurred, bool MatchesGuard);
}
