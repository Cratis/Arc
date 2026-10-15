// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

public class when_enforcing_method_body_regions : Specification
{
    UnitOfWork _unit;
    EventSequence _sequence;
    ServiceProvider _services;
    CommandContext _context;
    TransactionalCommandScope _scope;

    void Establish()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        _unit = unit;
        _sequence = new(store.Name, store.Namespace, EventSequenceId.Log, null!, null!, null!, null!, null!, null!, null!, null!, null!, new JsonSerializerOptions());
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(Arg.Any<Cratis.Execution.CorrelationId>()).Returns(_unit);
        _services = new ServiceCollection().AddSingleton(manager).AddSingleton(_sequence)
            .AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety()).BuildServiceProvider();
        _context = new(_unit.CorrelationId, typeof(AdvisoryCommand), new AdvisoryCommand(), [_sequence], new(), ServiceProvider: _services);
        _scope = new();
    }

    [Theory]
    [InlineData("GetNextSequenceNumber")]
    [InlineData("GetTailSequenceNumber")]
    [InlineData("GetTailSequenceNumberForObserver")]
    [InlineData("GetMetadataAt")]
    [InlineData("HasEventsFor")]
    public async Task should_refuse_raw_reads_before_transport_and_name_the_command(string operation)
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(AdvisoryCommand));
        _scope.Begin(_context);
        try
        {
            var failure = await Catch.Exception(async () =>
            {
                switch (operation)
                {
                    case "GetNextSequenceNumber": await _sequence.GetNextSequenceNumber(); break;
                    case "GetTailSequenceNumber": await _sequence.GetTailSequenceNumber(); break;
                    case "GetTailSequenceNumberForObserver": await _sequence.GetTailSequenceNumberForObserver(typeof(AdvisoryCommand)); break;
                    case "GetMetadataAt": await _sequence.GetMetadataAt((EventSequenceNumber)100ul); break;
                    case "HasEventsFor": await _sequence.HasEventsFor("source"); break;
                }
            });
            failure.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            new ReadProtectionCommandExceptionMapper().TryMap(_context, failure!, out var result).ShouldBeTrue();
            result!.ExceptionMessages.Single().ShouldEqual($"AdvisoryCommand called {operation} while read protection is On. Take a read model as a parameter, use IDecisionReads.Get in Provide, or mark the method [Unprotected(\"why\")].");
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_allow_raw_reads_only_inside_an_awaited_advisory_handle()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(AdvisoryCommand));
        _scope.Begin(_context);
        try
        {
            var method = typeof(AdvisoryCommand).GetMethod(nameof(AdvisoryCommand.Handle))!;
            var handler = new ModelBoundCommandHandler(typeof(AdvisoryCommand), method);
            var result = await handler.Handle(_context);
            result.ShouldEqual(0);
            (await Catch.Exception(_sequence.GetNextSequenceNumber)).ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            _unit.HasEnrolledDecisionReads.ShouldBeFalse();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_allow_raw_reads_only_inside_an_awaited_advisory_provide()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(AdvisoryCommand));
        _scope.Begin(_context);
        try
        {
            var result = await new CommandProvideInvoker().Invoke(_context, _services);
            result.Single().ShouldEqual(0);
            (await Catch.Exception(async () => await _sequence.GetTailSequenceNumber())).ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_never_allow_an_immediate_append_inside_an_advisory_body()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(AdvisoryCommand));
        _scope.Begin(_context);
        try
        {
            using var allowance = new DecisionDependencySafety().BeginMethodBody(typeof(AdvisoryCommand).GetMethod(nameof(AdvisoryCommand.Handle))!);
            var failure = await Catch.Exception(async () => await _sequence.Append("source", new object()));
            failure.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
            new ReadProtectionCommandExceptionMapper().TryMap(_context, failure!, out var result).ShouldBeTrue();
            result!.ExceptionMessages.Single().ShouldEqual("AdvisoryCommand appended events immediately through Append while read protection is On. Return the event from Handle, or use IEventLog.Transactional.");
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_keep_advisory_reads_but_refuse_raw_reads_and_appends_after_early_commit()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(AdvisoryCommand));
        _scope.Begin(_context);
        try
        {
            (await TransactionalCommandScope.CommitAggregate(_unit)).ShouldBeTrue();
            (await Catch.Exception(async () => await _sequence.GetTailSequenceNumber())).ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            (await Catch.Exception(async () => await _sequence.Append("source", new object()))).ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
            var handler = new ModelBoundCommandHandler(typeof(AdvisoryCommand), typeof(AdvisoryCommand).GetMethod(nameof(AdvisoryCommand.Handle))!);
            (await handler.Handle(_context)).ShouldEqual(0);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    void Destroy()
    {
        _services.Dispose();
        CommandTransaction.Current = null;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record AdvisoryCommand
    {
        [Unprotected("screen context only")]
        public async Task<int> Handle(EventSequence sequence)
        {
            await Task.Yield();
            return (await sequence.GetMetadataAt([])).Count;
        }

        [Unprotected("search hint only")]
        public async Task<int> Provide(EventSequence sequence)
        {
            await Task.Yield();
            return (await sequence.GetMetadataAt([])).Count;
        }
    }
}
