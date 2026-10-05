// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A command that names an event source definition and a stream says where its events go. What is recovered keeps the
/// definition's own name and stream rather than a string, and the concurrency scope the definition declares for the
/// stream is what the command is checked within unless the command states one itself.
/// </summary>
public class a_command_appending_through_an_event_source : Specification
{
    const string Source = """
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSources;

        namespace Library.Accounts;

        [EventType]
        public record FundsDeposited(decimal Amount);

        [EventSource(Concurrency = ConcurrencyDimensions.EventSourceId)]
        [EventStream("Transactions", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]
        [EventStream("Postings", Concurrency = ConcurrencyDimensions.EventStreamId)]
        [EventStream("Onboarding")]
        public class AccountEventSource : IEventSource;

        [Command]
        [EventSource<AccountEventSource>("Transactions")]
        public record Deposit(decimal Amount)
        {
            public FundsDeposited Handle() => new(Amount);
        }

        [Command]
        [EventSource<AccountEventSource>("Onboarding")]
        public record Onboard(decimal Amount)
        {
            public FundsDeposited Handle() => new(Amount);
        }

        [Command]
        [EventSource<AccountEventSource>("Postings")]
        public record Post(decimal Amount)
        {
            public FundsDeposited Handle() => new(Amount);
        }

        [Command]
        [EventSource<AccountEventSource>("Missing")]
        public record Misplace(decimal Amount)
        {
            public FundsDeposited Handle() => new(Amount);
        }

        [Command]
        [EventSource<AccountEventSource>("Transactions")]
        [EventStreamType("Ledger", true)]
        public record DepositWithAnExplicitScope(decimal Amount)
        {
            public FundsDeposited Handle() => new(Amount);
        }

        [Command]
        [EventStreamType("Ledger", true)]
        public record DepositWithoutAnEventSource(decimal Amount)
        {
            public FundsDeposited Handle() => new(Amount);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    Model.CommandModel Command(string name) => _analysis.Slice().Commands.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_keep_the_name_of_the_event_source() => Command("Deposit").EventSource!.Source.ShouldEqual("Account");
    [Fact] void should_keep_the_stream() => Command("Deposit").EventSource!.Stream.ShouldEqual("Transactions");
    [Fact] void should_know_the_stream_is_declared() => Command("Deposit").EventSource!.StreamDeclared.ShouldBeTrue();
    [Fact] void should_scope_by_the_event_source_id_the_stream_declares() => Command("Deposit").Concurrency!.EventSource.ShouldBeTrue();
    [Fact] void should_scope_by_the_stream_type_the_stream_declares() => Command("Deposit").Concurrency!.StreamType.ShouldEqual("Transactions");
    [Fact] void should_inherit_the_scope_of_the_event_source_for_a_stream_declaring_none() => Command("Onboard").Concurrency!.EventSource.ShouldBeTrue();
    [Fact] void should_not_invent_a_stream_type_the_scope_does_not_have() => Command("Onboard").Concurrency!.StreamType.ShouldBeNull();
    [Fact] void should_report_a_stream_id_dimension_it_cannot_state() => Command("Post").EventSource!.ConcurrentByStreamId.ShouldBeTrue();
    [Fact] void should_declare_no_scope_when_only_the_stream_id_takes_part() => Command("Post").Concurrency.ShouldBeNull();
    [Fact] void should_know_a_stream_the_definition_does_not_declare() => Command("Misplace").EventSource!.StreamDeclared.ShouldBeFalse();
    [Fact] void should_let_the_scope_the_command_states_win() => Command("DepositWithAnExplicitScope").Concurrency!.StreamType.ShouldEqual("Ledger");
    [Fact] void should_not_mix_the_definition_into_the_scope_the_command_states() => Command("DepositWithAnExplicitScope").Concurrency!.EventSource.ShouldBeFalse();
    [Fact] void should_recover_no_event_source_for_a_legacy_command() => Command("DepositWithoutAnEventSource").EventSource.ShouldBeNull();
    [Fact] void should_keep_the_legacy_scope() => Command("DepositWithoutAnEventSource").Concurrency!.StreamType.ShouldEqual("Ledger");
}
