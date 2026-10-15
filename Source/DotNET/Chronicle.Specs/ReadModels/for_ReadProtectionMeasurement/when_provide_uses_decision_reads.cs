// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_provide_uses_decision_reads : given.a_measure_pipeline
{
    readonly MeasureProvide _offProvider = new();
    readonly MeasureProvide _measureProvider = new();
    readonly List<object> _provided = [];
    IConcurrencyScopeStrategy _strategy;
    CommandResult _off;
    CommandResult _measure;

    void Establish()
    {
        _additionalCommands.Add(typeof(MeasureProvide).FullName!);
        _services.GetService(typeof(IDecisionReads)).Returns(new CommandDecisionReads(_reader, _store, Substitute.For<IReadModels>()));
        _strategy = Substitute.For<IConcurrencyScopeStrategy>();
        _strategy.GetScope(Arg.Any<EventSourceId>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(new ConcurrencyScope(90ul, (EventSourceId)"subject"));
    }

    async Task Because()
    {
        async Task Handle(CommandContext context)
        {
            _provided.AddRange(await new CommandProvideInvoker().Invoke(context, _services));
            await Commands.ConcurrencyScopeBuilder.BuildFor(context, _strategy, "subject");
            Stage(context);
        }
        _off = await Run(false, Handle, _offProvider);
        Assert.False(_off.HasExceptions, string.Join(Environment.NewLine, _off.ExceptionMessages) + _off.ExceptionStackTrace);
        _measure = await Run(true, Handle, _measureProvider);
        Assert.False(_measure.HasExceptions, string.Join(Environment.NewLine, _measure.ExceptionMessages) + _measure.ExceptionStackTrace);
    }

    [Fact] void should_call_each_provider_once() => (_offProvider.Calls, _measureProvider.Calls).ShouldEqual((1, 1));
    [Fact] void should_not_repeat_the_actual_reads() => _reader.Acquisitions.ShouldEqual(2);
    [Fact] void should_not_repeat_the_concurrency_strategy() => _strategy.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IConcurrencyScopeStrategy.GetScope)).ShouldEqual(2);
    [Fact] void should_keep_provided_values() => _provided.ShouldEqual(new object[] { new Stock("subject"), new Stock("subject") });
    [Fact] void should_keep_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_scopes() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_report_the_detached_call_hypothesis() => _telemetry.ShouldContain(_ => _.Category == "refused_read" && _.Reason == "DetachedReadWhileReadsAreProtected");

    [EventStreamType("approval", concurrency: true)]
    public class OffProvide
    {
        public int Calls { get; private set; }
        public async Task<Stock> Provide(IDecisionReads reads)
        {
            Calls++;
            await reads.GetDetached<Stock>("subject");
            return new("subject");
        }
    }

    [ReadProtection(ReadProtectionMode.Measure)]
    [EventStreamType("approval", concurrency: true)]
    public class MeasureProvide : OffProvide;
}
