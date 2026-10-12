// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.given;

public class a_definition_routed_command : Specification
{
    protected IEventSources _eventSources;
    protected IEventLog _eventLog;
    protected IUnitOfWork _unitOfWork;
    protected CommandContext _context;
    protected EventForEventSourceId _invalid;
    protected EventForEventSourceId _valid;

    void Establish()
    {
        _eventSources = Substitute.For<IEventSources>();
        _eventSources.All.Returns([
            new EventSourceDefinition(typeof(Account), "Account", "", ConcurrencyDimensions.None, [new("Transactions", "", ConcurrencyDimensions.None), new("Adjustments", "", ConcurrencyDimensions.None)]),
            new EventSourceDefinition(typeof(Ledger), "Ledger", "", ConcurrencyDimensions.None, [new("Entries", "", ConcurrencyDimensions.None)])]);
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.Id.Returns(EventSequenceId.Log);
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _context = new(CorrelationId.New(), typeof(object), new object(), [], new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSource, typeof(Account) },
            { WellKnownCommandContextKeys.EventStream, "Transactions" },
            { WellKnownCommandContextKeys.EventSourceType, (EventSourceType)"Account" },
            { WellKnownCommandContextKeys.EventStreamType, (EventStreamType)"Transactions" },
            { WellKnownCommandContextKeys.EventSourceId, (EventSourceId)"command" }
        });
        _invalid = new("id", new object()) { EventSourceType = "Account", EventStreamType = "Typo" };
        _valid = new("id", new object()) { EventSource = typeof(Account), EventStream = "Transactions" };
    }

    void Destroy() => CommandTransaction.Current = null;

    protected ServiceProvider ServicesFor<THandler>()
        where THandler : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(_eventLog);
        services.AddSingleton(_eventSources);
        services.AddSingleton(Substitute.For<IEventTypes>());
        services.AddSingleton(Substitute.For<IConcurrencyScopeStrategies>());
        services.AddTransient<THandler>();

        return services.BuildServiceProvider();
    }

    protected void ShouldHaveNoEffects()
    {
        _unitOfWork.ReceivedCalls().Where(call => call.GetMethodInfo().Name.StartsWith("Add", StringComparison.Ordinal)).ShouldBeEmpty();
        _eventLog.ReceivedCalls().Where(call => call.GetMethodInfo().Name.StartsWith("Append", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    public class Account;
    public class Ledger;
}
