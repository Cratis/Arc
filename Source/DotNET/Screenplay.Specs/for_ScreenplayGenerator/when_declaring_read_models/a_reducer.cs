// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model a reducer folds events into, which is not marked as a read model at all - the reducer naming it is
/// the only thing that says it is one.
/// </summary>
public class a_reducer : a_read_model_document
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reducers;

        namespace Library.Lending.Balances;

        [EventType]
        public record BookReserved(string Isbn);

        public record Balance(int Outstanding);

        public class BalanceReducer : IReducerFor<Balance>
        {
            public Task<Balance> Reserved(BookReserved @event, Balance? current, EventContext context) =>
                Task.FromResult(new Balance((current?.Outstanding ?? 0) + 1));
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_declare_the_read_model() => Says("readmodel Balance").ShouldBeTrue();
    [Fact] void should_declare_what_it_holds() => Says("outstanding Int").ShouldBeTrue();
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => RoundTrip.IsStable.ShouldBeTrue();
}
