// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

public class with_a_const_filtered_reducer : a_generated_document
{
    void Because() => Generate(("Library/Lending/Balances/Balance.cs", """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSources;
        using Cratis.Chronicle.Reducers;
        namespace Library.Lending.Balances;
        [EventSource("stored-account")]
        [EventStream(AccountEventSource.Notes)]
        public class AccountEventSource : IEventSource
        {
            public const string Notes = "stored-notes";
        }
        [EventType] public record Noted(string Id);
        [Cratis.Arc.Queries.ModelBound.ReadModel]
        public record Balance(string Id, int Count)
        {
            public static Balance? ById(string id) => new(id, 0);
        }
        [FromEventSource<AccountEventSource>(AccountEventSource.Notes)]
        public class BalanceReducer : IReducerFor<Balance>
        {
            public Task<Balance> Noted(Noted @event, Balance? current, EventContext context) =>
                Task.FromResult(new Balance(@event.Id, (current?.Count ?? 0) + 1));
        }
        """));

    [Fact] void should_recover_the_observed_source_name() => Result.Model.Slices.SelectMany(slice => slice.Projections).Single().EventSource!.Source.ShouldEqual("Account");
    [Fact] void should_recover_the_observed_stream_name() => Result.Model.Slices.SelectMany(slice => slice.Projections).Single().EventSource!.Stream.ShouldEqual("Notes");
    [Fact] void should_preserve_the_existing_fold_limitation() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ReducerWithoutCounterpart);
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
