// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_property_path_mapping : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public record Address(string Month);
        [Command, EventSourceType("Account"), EventStreamType("Transactions")]
        public record RegisterAuthor(AuthorId AuthorId, Address Address, string Name) : ICanProvideEventStreamId
        {
            public EventStreamId GetEventStreamId() => Address.Month;
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_withhold_the_entire_unadmitted_route() => Result.Source.ShouldNotContain("eventsource Account");
    [Fact] void should_report_the_admission_reason() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Message.ShouldContain("property-path route mappings are not admitted (PLAY0268)");
    [Fact] void should_report_information_only() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
