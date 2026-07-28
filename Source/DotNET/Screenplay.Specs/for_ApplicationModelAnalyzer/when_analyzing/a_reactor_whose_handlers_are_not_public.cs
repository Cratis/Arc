// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Chronicle dispatches to every instance method whose first parameter is an event type, public or not. A reactor
/// keeping its handlers to itself therefore observes those events at runtime, and a document leaving them out would
/// describe a reactor that reacts to nothing.
/// </summary>
public class a_reactor_whose_handlers_are_not_public : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Lending.Notifications;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record ReservationExpired(string Isbn);

        public class ReservationNotifier : IReactor
        {
            public Task BookReserved(BookReserved @event, EventContext context) => Task.CompletedTask;

            internal Task ReservationExpired(ReservationExpired @event, EventContext context) => Task.CompletedTask;
        }
        """;

    ApplicationModelAnalysis _analysis;
    ReactorModel _reactor;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _reactor = _analysis.Slice().Reactors.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_observe_every_event_it_handles() => _reactor.ObservedEvents.ShouldContainOnly(["BookReserved", "ReservationExpired"]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
