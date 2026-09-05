// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A handler that builds its event somewhere else gives the reader nothing to map from. The signature still promises
/// which event comes out, so that much is stated and the missing mappings are reported - which is exactly the
/// fidelity an artifact living in a referenced package degrades to, where there is metadata but no body at all.
/// </summary>
public class a_handler_whose_body_cannot_be_read : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        public static class Events
        {
            public static AuthorRegistered For(string name) => new(name);
        }

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => Events.For(Name);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_state_the_event_the_signature_promises() => _analysis.Slice().Commands.First().Produces.Single().EventName.ShouldEqual("AuthorRegistered");
    [Fact] void should_state_it_without_mappings() => _analysis.Slice().Commands.First().Produces.Single().Mappings.ShouldBeEmpty();
    [Fact] void should_state_it_unconditionally() => _analysis.Slice().Commands.First().Produces.Single().When.ShouldBeNull();
    [Fact] void should_report_that_the_mappings_are_missing() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableCommandProduction]);
    [Fact] void should_say_why() => _analysis.Diagnostics.Single().Message.Contains("never constructed in a body that could be read", StringComparison.Ordinal).ShouldBeTrue();
}
