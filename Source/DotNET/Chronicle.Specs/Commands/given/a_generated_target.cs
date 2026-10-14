// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Concepts;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.given;

public class a_generated_target : Specification
{
    protected CommandContext _context;
    protected EventSourceId _id;
    internal FreshIdentifierScopes _freshness;
    protected IEventLog _eventLog;
    protected IEventTypes _eventTypes;
    protected IEventSources _eventSources;
    protected IConcurrencyScopeStrategies _strategies;
    protected RecordingUnitOfWork _unit;
    protected CommandResult _result;
    protected ConcurrencyScope _scope;
    protected EventForEventSourceId[] _enrolled;
    protected KeyValuePair<EventSourceId, ConcurrencyScope>[] _enrolledScopes;

    async Task Establish()
    {
        await Capture<CreatesId>();
        _id = new(Identifier.Fixed);
        _freshness = FreshIdentifierScopes.For(_context);
        _context.Values[WellKnownCommandContextKeys.EventSourceId] = _id;
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.Id.Returns(EventSequenceId.Log);
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.HasFor(typeof(Created)).Returns(true);
        _eventSources = Substitute.For<IEventSources>();
        _strategies = Substitute.For<IConcurrencyScopeStrategies>();
        _strategies.GetFor(_eventLog).Returns(Substitute.For<IConcurrencyScopeStrategy>());
        _unit = new RecordingUnitOfWork
        {
            Enrollment = (_, events, scopes) =>
            {
                _enrolled = events;
                _enrolledScopes = scopes;
            }
        };
    }

    protected async Task Capture<T>()
        where T : new()
    {
        var provide = Substitute.For<ICommandProvideInvoker>();
        provide.Invoke(Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>()).Returns(new ValueTask<IReadOnlyList<object>>([]));
        var resolver = new CommandHandlerArgumentResolver(provide);
        _context = new(CorrelationId.New(), typeof(T), new T(), [], new());
        var handler = new ModelBoundCommandHandler(typeof(T), typeof(T).GetMethod("Handle")!);
        await resolver.Resolve(handler, _context, Substitute.For<IServiceProvider>(), null);
    }

    void Destroy() => CommandTransaction.Current = null;

    public record Identifier(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<Identifier>
    {
        public static readonly Guid Fixed = new(0xda8fe3d9, 0xa6b7, 0x47ba, 0x88, 0x7a, 0x7b, 0x5a, 0x57, 0x3e, 0xf6, 0x17);
        public static Identifier New() => new(Fixed);
    }

    public class CreatesId
    {
        public void Handle([GeneratedValue] Identifier id) { }
    }

    public class CreatesUnguardedId
    {
        public void Handle([GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)] Identifier id) { }
    }

    public class CreatesMixedPolicyIds
    {
        public void Handle([GeneratedValue] Identifier guarded, [GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)] Identifier unguarded) { }
    }

    [EventType("Arc.Chronicle.Specs.GeneratedTargetRecorded")]
    public record Created(string Name);
}
