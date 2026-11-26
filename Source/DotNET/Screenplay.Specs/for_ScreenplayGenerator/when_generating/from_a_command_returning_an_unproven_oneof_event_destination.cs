// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_returning_an_unproven_oneof_event_destination : a_generated_document
{
    [Theory]
    [InlineData("OneOf<EventForEventSourceId, ValidationResult>.FromT0(new EventForEventSourceId(Id + \"-other\", new Recorded(view.Value)))")]
    [InlineData("new EventForEventSourceId(Id + \"-other\", new Recorded(view.Value))")]
    [InlineData("Wrap(new EventForEventSourceId(Id, new Recorded(view.Value)))")]
    public void should_not_claim_an_unproven_destination_is_the_command_identifier(string returned)
    {
        var source = from_a_stage_shaped_command_keyed_by_its_identifier.Source(true)
            .Replace("OneOf<EventForEventSourceId, ValidationResult>.FromT0(new EventForEventSourceId(Id, new Recorded(view.Value)))", returned, StringComparison.Ordinal)
            .Replace("public EventSourceId GetEventSourceId() => Id;", "public EventSourceId GetEventSourceId() => Id;\npublic static OneOf<EventForEventSourceId, ValidationResult> Wrap(EventForEventSourceId value) => value;", StringComparison.Ordinal);
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            var production = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single();
            production.Destination.ShouldBeNull();
            production.UsesCommandContext.ShouldBeFalse();
            Result.Source.ShouldNotContain("for id");
            Result.Source.ShouldNotContain(" identifier");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
            Analyzed.ErrorsIn(Analyzed.Compile((Analyzed.SlicePath, source))).ShouldBeEmpty();
        }
    }
}
