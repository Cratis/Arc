// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_a_command_filter_reads : given.a_protected_read
{
    [Fact]
    public async Task should_refuse_a_read_model_injected_into_a_filter_and_name_the_fix()
    {
        await Seed();
        await using var provider = FilterServices();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var failure = Catch.Exception(() => scope.ServiceProvider.GetRequiredService<StockFilter>());
            failure.ShouldBeOfExactType<ReadModelInjectedOutsideDecision>();
            failure.Message.ShouldEqual("Borrow resolved BookStock through dependency injection outside Handle() and its validators, for example in a command filter, so read protection cannot guard it. Take BookStock as a parameter of Handle() or of a CommandValidator<Borrow> constructor. If it is only context, mark BookStock [Unprotected(\"why\")].");
            _unit.HasEnrolledDecisionReads.ShouldBeFalse();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_read_an_advisory_model_injected_into_a_filter_without_guarding_it()
    {
        await Seed();
        await using var provider = FilterServices();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            await using var scope = provider.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<LoansFilter>().Loans.Count.ShouldEqual(2);
            _unit.HasEnrolledDecisionReads.ShouldBeFalse();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_keep_a_read_model_injected_into_a_filter_a_plain_read_outside_on()
    {
        await Seed();
        await using var provider = FilterServices();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(OffBorrow));
        Begin(new OffBorrow(_book));
        try
        {
            await using var scope = provider.CreateAsyncScope();
            Catch.Exception(() => scope.ServiceProvider.GetRequiredService<StockFilter>()).ShouldBeNull();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_refuse_raw_reads_a_filter_makes_while_it_executes()
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var readModels = await Catch.Exception(async () => await _store.ReadModels.GetInstanceById<BookStock>((ReadModelKey)_book.Value));
            readModels.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            var eventLog = await Catch.Exception(async () => await _store.EventLog.GetTailSequenceNumber());
            eventLog.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            new ReadProtectionCommandExceptionMapper().TryMap(_context, readModels!, out var mapped).ShouldBeTrue();
            mapped!.ExceptionMessages.Single().ShouldContain("Borrow called ");
            _unit.HasEnrolledDecisionReads.ShouldBeFalse();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    ServiceProvider FilterServices()
    {
        // BookStock is read from the store, which refuses raw reads while reads are protected. The advisory model is
        // not a Chronicle projection; reading it probes the store with a raw read, which only succeeds in an advisory region.
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstanceById(typeof(BookStock), Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(call => _store.ReadModels.GetInstanceById(typeof(BookStock), call.Arg<ReadModelKey>()));
        readModels.Release(Arg.Any<BookStock>()).Returns(call => Task.FromResult(call.Arg<BookStock>()));
        readModels.GetInstanceById(typeof(AdvisoryLoans), Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(async _ =>
            {
                await _store.EventLog.GetTailSequenceNumber();
                return (object)new AdvisoryLoans(_member, 2);
            });
        readModels.Release(Arg.Any<AdvisoryLoans>()).Returns(call => Task.FromResult(call.Arg<AdvisoryLoans>()));
        return new ServiceCollection()
            .AddSingleton(_services.GetRequiredService<ICommandContextAccessor>())
            .AddTransient(sp => sp.GetRequiredService<ICommandContextAccessor>().Current)
            .AddSingleton(readModels)
            .AddReadModelsForCommand(new ChronicleReadModelForCommandResolver([typeof(BookStock), typeof(AdvisoryLoans)]))
            .AddTransient<StockFilter>()
            .AddTransient<LoansFilter>()
            .BuildServiceProvider();
    }

    public record OffBorrow(BookId Id);

    [Unprotected("shown to the member only")]
    public record AdvisoryLoans(MemberId Id, int Count);

    /// <summary>
    /// A stand-in for a command filter: it takes its dependencies the way a filter does, but does not implement
    /// ICommandFilter, so type discovery does not add it to the filters of every other command scenario in this assembly.
    /// </summary>
    /// <param name="stock">The injected read model.</param>
    public class StockFilter(BookStock stock)
    {
        public BookStock Stock { get; } = stock;
    }

    /// <summary>
    /// A stand-in for a command filter that injects an advisory read model.
    /// </summary>
    /// <param name="loans">The injected read model.</param>
    public class LoansFilter(AdvisoryLoans loans)
    {
        public AdvisoryLoans Loans { get; } = loans;
    }
}
