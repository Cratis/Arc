// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender;

public class when_deferring_declared_concurrency : Specification
{
    IConcurrencyScopeStrategy _strategy;
    IUnitOfWork _unit;
    EventEnrollmentOptions _options;
    ConcurrencyScope _scope;

    void Establish()
    {
        _strategy = Substitute.For<IConcurrencyScopeStrategy>();
        _unit = Substitute.For<UnitOfWorkWithNamedTags>();
        _unit.IsEnforcingReadProtection.Returns(true);
        _unit.When(_ => _.AddEventWithOptions(Arg.Any<EventSequenceId>(), Arg.Any<Cratis.Chronicle.Events.EventSourceId>(), Arg.Any<object>(), Arg.Any<Cratis.Chronicle.Auditing.Causation>(), Arg.Any<EventEnrollmentOptions>()))
            .Do(call => _options = call.Arg<EventEnrollmentOptions>());
    }

    async Task Because()
    {
        CommandTransaction.Current = _unit;
        var context = new CommandContext(CorrelationId.New(), typeof(DeclaredCommand), new DeclaredCommand(), [], new());
        _scope = await ConcurrencyScopeBuilder.BuildFor(context, _strategy, "source");
        var log = Substitute.For<IEventLog>();
        log.Id.Returns(EventSequenceId.Log);
        log.TryEnrollForCommand("source", new object(), context, _scope);
    }

    void Destroy() => CommandTransaction.Current = null;
    [Fact] void should_not_acquire_a_tail() => _strategy.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_build_an_explicit_scope() => _scope.ShouldBeNull();
    [Fact] void should_mark_routing_provenance() => _options.Origin.ShouldEqual(ConcurrencyScopeOrigin.Routing);
    [Fact] void should_forward_the_declared_dimensions() => _options.DeclaredConcurrency.ShouldEqual(ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType);

    [EventStreamType("stream", concurrency: true)]
    public record DeclaredCommand;
}
