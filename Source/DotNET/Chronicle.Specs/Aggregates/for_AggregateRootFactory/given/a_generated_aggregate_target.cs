// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.given;

public class a_generated_aggregate_target : Commands.given.a_generated_target
{
    protected AggregateRootFactory _factory;
    protected IEventSequence _sequence;
    protected IAggregateRootMutatorFactory _mutatorFactory;
    protected IAggregateRootMutator _mutator;
    protected TransactionalCommandScope _execution;
    protected NewAggregate _aggregate;
    protected Exception? _error;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _sequence = Substitute.For<IEventSequence>();
        _sequence.Id.Returns(EventSequenceId.Log);
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.HasCurrent.Returns(true);
        manager.Current.Returns(_unit);
        _mutator = Substitute.For<IAggregateRootMutator>();
        _mutatorFactory = Substitute.For<IAggregateRootMutatorFactory>();
        _mutatorFactory.Create<NewAggregate>(Arg.Any<IAggregateRootContext>()).Returns(_mutator);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEventSources)).Returns(_eventSources);
        _factory = new(store, _mutatorFactory, manager, services);
        _execution = new();
    }

    protected async Task<T> InInvocation<T>(Func<Task<T>> action)
    {
        _execution.Begin(_context);
        try
        {
            return await action();
        }
        finally
        {
            await _execution.Complete(_context, CommandResult.Success(_context.CorrelationId));
        }
    }

    public class NewAggregate : AggregateRoot
    {
        public bool Activated { get; private set; }

        protected override Task OnActivate()
        {
            Activated = true;
            return Task.CompletedTask;
        }
    }
}
