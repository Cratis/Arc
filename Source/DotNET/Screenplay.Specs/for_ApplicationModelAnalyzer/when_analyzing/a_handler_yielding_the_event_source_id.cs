// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A handler can decide which event source it appends to by returning the identifier alongside the event. Screenplay
/// has no way of saying that, so the event still has to be recovered and the part that is lost has to be reported
/// rather than quietly dropped.
/// </summary>
public class a_handler_yielding_the_event_source_id : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Id, string Name)
        {
            public (EventSourceId, AuthorRegistered) Handle() => (Id, new AuthorRegistered(Name));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_produce_the_event() => _analysis.Slice().Commands.First().Produces.Single().EventName.ShouldEqual("AuthorRegistered");
    [Fact] void should_still_map_its_properties() => _analysis.Slice().Commands.First().Produces.Single().Mappings.ShouldNotBeEmpty();
    [Fact] void should_report_what_it_cannot_say() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult]);
    [Fact] void should_report_it_as_information_only() => _analysis.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
}
