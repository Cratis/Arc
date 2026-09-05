// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A reactor that only causes an effect outside the system is an automation. Holding a command pipeline without ever
/// using it does not change that, which is exactly what a constructor-dependency heuristic would get wrong.
/// </summary>
public class a_reactor_causing_an_effect : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Lending.Notifications;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record ReservationExpired(string Isbn);

        public class ReservationNotifier(ICommandPipeline pipeline) : IReactor
        {
            public Task BookReserved(BookReserved @event, EventContext context) => Task.CompletedTask;

            public Task ReservationExpired(ReservationExpired @event, EventContext context) => Task.CompletedTask;
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
    [Fact] void should_name_the_reactor() => _reactor.Name.ShouldEqual("ReservationNotifier");
    [Fact] void should_observe_the_events_its_methods_take() => _reactor.ObservedEvents.ShouldContainOnly(["BookReserved", "ReservationExpired"]);
    [Fact] void should_not_call_it_translating() => _reactor.IsTranslating.ShouldBeFalse();
    [Fact] void should_recover_the_file_it_lives_in() => _reactor.SourceFilePath.ShouldEqual("Feature/Slice/Slice.cs");
    [Fact] void should_infer_an_automation_slice() => _analysis.Slice().Kind.ShouldEqual(SliceKind.Automation);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
