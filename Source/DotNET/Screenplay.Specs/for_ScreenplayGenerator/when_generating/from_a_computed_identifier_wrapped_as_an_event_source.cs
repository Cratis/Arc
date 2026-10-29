// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_computed_identifier_wrapped_as_an_event_source : a_generated_document
{
    [Theory]
    [InlineData("new EventSourceId((Id + OtherId).ToString())")]
    [InlineData("new EventSourceId(Read(Id))")]
    public void should_reject_computed_identity_and_destination(string expression)
    {
        var source = GeneratedIdentifierSources.With("string", expression, expression).Replace(
            "public EventSourceId GetEventSourceId()", "static string Read(string value) => value; public EventSourceId GetEventSourceId()", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain(" identifier");
        var diagnostic = Result.Diagnostics.Single(item => item.Code == ScreenplayDiagnosticCodes.UnreadableCommandIdentifier);
        diagnostic.Location.ShouldNotBeNull();
        Result.Diagnostics.Any(item => item.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        AssertPartialDocument();
    }
}
