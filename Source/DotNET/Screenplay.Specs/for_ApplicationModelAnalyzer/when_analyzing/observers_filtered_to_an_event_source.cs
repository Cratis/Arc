// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A reactor or reducer filtered with the typed <c>[FromEventSource&lt;TSource&gt;(stream)]</c> keeps the definition's
/// own name and the stream rather than losing them, whether or not the definition declares the stream. An observer
/// that is not filtered is read exactly as before.
/// </summary>
public class observers_filtered_to_an_event_source : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSources;
        using Cratis.Chronicle.Reactors;
        using Cratis.Chronicle.Reducers;

        namespace Library.Accounts;

        [EventType]
        public record FundsDeposited(decimal Amount);

        [EventSource]
        [EventStream("Transactions")]
        public class AccountEventSource : IEventSource;

        [ReadModel]
        public record Balance(decimal Total);

        [FromEventSource<AccountEventSource>("Transactions")]
        public class Notifier : IReactor
        {
            public Task Deposited(FundsDeposited @event, EventContext context) => Task.CompletedTask;
        }

        [FromEventSource<AccountEventSource>("Missing")]
        public class Misdirected : IReactor
        {
            public Task Deposited(FundsDeposited @event, EventContext context) => Task.CompletedTask;
        }

        public class Unfiltered : IReactor
        {
            public Task Deposited(FundsDeposited @event, EventContext context) => Task.CompletedTask;
        }

        [FromEventSource<AccountEventSource>("Transactions")]
        public class BalanceReducer : IReducerFor<Balance>
        {
            public Task<Balance> Deposited(FundsDeposited @event, Balance? current, EventContext context) =>
                Task.FromResult(new Balance((current?.Total ?? 0) + @event.Amount));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ReactorModel Reactor(string name) => _analysis.Slice().Reactors.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_keep_the_name_of_the_event_source() => Reactor("Notifier").EventSource!.Source.ShouldEqual("Account");
    [Fact] void should_keep_the_stream() => Reactor("Notifier").EventSource!.Stream.ShouldEqual("Transactions");
    [Fact] void should_know_the_stream_is_declared() => Reactor("Notifier").EventSource!.StreamDeclared.ShouldBeTrue();
    [Fact] void should_still_observe_its_events() => Reactor("Notifier").ObservedEvents.ShouldContainOnly(["FundsDeposited"]);
    [Fact] void should_know_a_stream_the_definition_does_not_declare() => Reactor("Misdirected").EventSource!.StreamDeclared.ShouldBeFalse();
    [Fact] void should_recover_no_event_source_for_an_unfiltered_reactor() => Reactor("Unfiltered").EventSource.ShouldBeNull();
    [Fact] void should_keep_the_event_source_of_a_reducer() => _analysis.Slice().Projections.Single().EventSource!.Stream.ShouldEqual("Transactions");
}
