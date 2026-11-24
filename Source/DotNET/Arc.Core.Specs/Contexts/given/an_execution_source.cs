// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Contexts.given;

public class an_execution_source : Specification
{
    protected static readonly DateTimeOffset Receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    protected readonly List<AvailabilityInvocation> _reads = [];
    protected ServiceProvider _services;
    protected AvailabilityInvocation _gated;
    protected CancellationToken _readCancellation;
    protected CommandContext _command;
    protected QueryContext _query;
    protected IQueryPerformerProviders _performers;

    void Establish()
    {
        var source = Substitute.For<IExecutionContextValueSource>();
        source.Read(Arg.Any<Type?>(), "value", Arg.Any<CancellationToken>()).Returns(ContextValueRead.Absent);
        source.ReadForExecution(Arg.Any<AvailabilityInvocation>(), "value", Arg.Any<CancellationToken>()).Returns(call =>
        {
            _reads.Add(call.Arg<AvailabilityInvocation>());
            _readCancellation = call.Arg<CancellationToken>();
            return ContextValueRead.Present("bound");
        });
        var gate = Substitute.For<IAvailabilityExecutionGate>();
        gate.IsGated(Arg.Any<Type>()).Returns(true);
        gate.Decide(Arg.Any<AvailabilityInvocation>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _gated = call.Arg<AvailabilityInvocation>();
            return AvailabilityDecision.Available;
        });
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(Command));
        _performers = Substitute.For<IQueryPerformerProviders>();
        _performers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        _services = new ServiceCollection().AddSingleton<IContextValueSource>(source).AddSingleton<IAvailabilityGate>(gate)
            .AddSingleton(_performers).BuildServiceProvider();
        _command = new(CorrelationId.New(), typeof(Command), new Command(42), [], new CommandContextValues(), ServiceProvider: _services) { ReceivedAt = Receipt };
        _query = new("Test.Query", CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["count"] = 42 }, ServiceProvider: _services) { ReceivedAt = Receipt };
    }

    void Destroy() => _services.Dispose();

    public record Command(int Count);
}
