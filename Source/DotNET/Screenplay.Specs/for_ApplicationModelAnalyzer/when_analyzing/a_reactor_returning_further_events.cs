// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Returning an event rather than nothing is the other way a reactor translates, and it needs no body reading at all
/// - the signature already says the reaction produces something.
/// </summary>
public class a_reactor_returning_further_events : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Lending.Restocking;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record RestockRequested(string Isbn);

        public class Restocking : IReactor
        {
            public Task<RestockRequested> BookReserved(BookReserved @event, EventContext context) =>
                Task.FromResult(new RestockRequested(@event.Isbn));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_call_it_translating() => _analysis.Slice().Reactors.Single().IsTranslating.ShouldBeTrue();
    [Fact] void should_infer_a_translate_slice() => _analysis.Slice().Kind.ShouldEqual(SliceKind.Translate);
    [Fact] void should_observe_the_event_it_reacts_to() => _analysis.Slice().Reactors.Single().ObservedEvents.ShouldContainOnly(["BookReserved"]);
}
