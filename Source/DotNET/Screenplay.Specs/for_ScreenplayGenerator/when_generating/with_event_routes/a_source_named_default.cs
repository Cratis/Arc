// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_source_named_default : a_generated_document
{
    const string Source = """
        [Command, EventSourceType("Default"), EventStreamType("Transactions")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_withhold_the_complete_reserved_route_in_both_modes(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, IdentifierSources.With(Source)));

        Result.Source.ShouldNotContain("eventsource Default");
        Result.Source.ShouldNotContain("stream Default.Transactions");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute);
        diagnostic.Message.ShouldContain("reserved stored name (PLAY0273)");
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
        AssertDocument();
    }
}
