// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given;

public static class direct_write_commands
{
    public static readonly DateTimeOffset Occurred = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
    public static readonly Subject Subject = "direct-writer";
    public static readonly NamedTag[] NamedTags = [new("purpose", "freshness")];

    public enum WriteStyle
    {
        AddEvent = 0,
        AddEvents = 1,
        AddEventWithNamedTags = 2,
        Append = 3,
        AppendMany = 4,
        AppendWithNamedTags = 5,
        AppendManyWithNamedTags = 6
    }

    public static async Task Write(WriteStyle style, Identifier id, IEventLog log, IUnitOfWorkManager manager, ConcurrencyScope? scope = null)
    {
        var events = new object[] { new Created("first"), new Created("second") };
        switch (style)
        {
            case WriteStyle.AddEvents:
                manager.Current.AddEvents(log.Id, events.Select(@event => new EventForEventSourceId(id, @event) { Occurred = Occurred, Subject = Subject, NamedTags = NamedTags }), scope is null ? [] : [new KeyValuePair<EventSourceId, ConcurrencyScope>(id, scope)]);
                break;
            case WriteStyle.AppendMany:
                await log.Transactional.AppendMany(id, events, concurrencyScope: scope, occurred: Occurred, subject: Subject);
                break;
            case WriteStyle.AppendManyWithNamedTags:
                await log.Transactional.AppendManyWithNamedTags(id, events, NamedTags, concurrencyScope: scope, occurred: Occurred, subject: Subject);
                break;
            default:
                foreach (var @event in events)
                {
                    switch (style)
                    {
                        case WriteStyle.AddEvent:
                            manager.Current.AddEvent(log.Id, id, @event, Causation.Unknown(), concurrencyScope: scope, occurred: Occurred, subject: Subject);
                            break;
                        case WriteStyle.AddEventWithNamedTags:
                            manager.Current.AddEventWithNamedTags(log.Id, id, @event, NamedTags, Causation.Unknown(), concurrencyScope: scope, occurred: Occurred, subject: Subject);
                            break;
                        case WriteStyle.Append:
                            await log.Transactional.Append(id, @event, concurrencyScope: scope, occurred: Occurred, subject: Subject);
                            break;
                        case WriteStyle.AppendWithNamedTags:
                            await log.Transactional.AppendWithNamedTags(id, @event, NamedTags, concurrencyScope: scope, occurred: Occurred, subject: Subject);
                            break;
                    }
                }
                break;
        }
    }

    [Command]
    public record DirectWrite(WriteStyle Style, bool Conflict = false, bool Unrelated = false, bool EqualScope = false)
    {
        public int Calls { get; private set; }
        public async Task Handle([GeneratedValue] Identifier id, IEventLog log, IUnitOfWorkManager manager)
        {
            Calls++;
            var target = Unrelated ? new Identifier(new Guid(0xb89aa3eb, 0x0315, 0x4858, 0xb7, 0x3d, 0xae, 0x14, 0x6e, 0x9f, 0x78, 0xf6)) : id;
            var scope = Conflict ? ConcurrencyScope.None : EqualScope ? new ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: target) : null;
            await Write(Style, target, log, manager, scope);
        }
    }

    [Command]
    public record ConflictingRepeatedWrite(bool Batch = false)
    {
        public int EnrollmentCallbacks { get; private set; }
        public void Handle([GeneratedValue] Identifier id, IEventLog log, IUnitOfWorkManager manager)
        {
            var counter = new EnrollmentCounter();
            using var registration = manager.Current.AddEnrollmentContributor(counter);
            try
            {
                manager.Current.AddEvent(log.Id, id, new Created("first"), Causation.Unknown());
                if (Batch)
                {
                    manager.Current.AddEvents(log.Id, [new(id, new Created("conflict"))], [new KeyValuePair<EventSourceId, ConcurrencyScope>(id, ConcurrencyScope.None)]);
                }
                else
                {
                    manager.Current.AddEvent(log.Id, id, new Created("conflict"), Causation.Unknown(), concurrencyScope: ConcurrencyScope.None);
                }
            }
            finally
            {
                EnrollmentCallbacks = counter.Calls;
            }
        }
    }

    [Command]
    public record UnguardedDirectWrite(WriteStyle Style)
    {
        public Task Handle([GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)] Identifier id, IEventLog log, IUnitOfWorkManager manager) => Write(Style, id, log, manager);
    }

    [Command]
    public record MixedDirectWrite
    {
        public async Task<EventForEventSourceId> Handle([GeneratedValue] Identifier id, IEventLog log, IUnitOfWorkManager manager)
        {
            await Write(WriteStyle.AddEvents, id, log, manager);

            return new(id, new Created("returned"));
        }
    }

    [Command]
    public record TwoDirectSources
    {
        public async Task Handle([GeneratedValue] Identifier id, [GeneratedValue] Identifier second, IEventLog log, IUnitOfWorkManager manager)
        {
            await Write(WriteStyle.AddEvent, id, log, manager);
            await Write(WriteStyle.AddEvents, second, log, manager);
        }
    }

    [Command]
    public record NestedDirectWrite
    {
        public Task<CommandResult> Handle([GeneratedValue] Identifier id, ICommandPipeline pipeline) => pipeline.Execute(new InnerWrite(id));
    }

    [Command]
    public record InnerWrite(Identifier Id)
    {
        public Task Handle(IEventLog log, IUnitOfWorkManager manager) => Write(WriteStyle.AppendMany, Id, log, manager);
    }

    sealed class EnrollmentCounter : IUnitOfWorkEnrollmentContributor
    {
        public int Calls { get; private set; }
        public ConcurrencyScope GetRequiredScope(UnitOfWorkEnrollment enrollment)
        {
            Calls++;

            return ConcurrencyScope.NotSet;
        }
    }
}
