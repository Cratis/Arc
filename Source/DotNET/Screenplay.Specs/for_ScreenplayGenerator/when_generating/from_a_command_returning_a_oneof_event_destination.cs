// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_returning_a_oneof_event_destination : a_generated_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_the_identifier_and_protected_read_destination(bool implicitReturn)
    {
        var source = from_a_stage_shaped_command_keyed_by_its_identifier.Source(true);
        if (implicitReturn)
        {
            source = source.Replace("OneOf<EventForEventSourceId, ValidationResult>.FromT0(new EventForEventSourceId(Id, new Recorded(view.Value)))", "new EventForEventSourceId(Id, new Recorded(view.Value))", StringComparison.Ordinal);
        }
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Identifier.ShouldEqual("Id");
            command.Produces.Single().UsesCommandContext.ShouldBeTrue();
            Result.Source.ShouldContain("id String identifier");
            string.Join(' ', Result.Source.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)).ShouldContain("produces Recorded for id");
            Result.Source.ShouldContain("reads View by id");
            Result.Source.ShouldContain("require View.value != \"\"");
            Result.Source.ShouldContain("value = View.value");
            Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeEmpty();
            Bound.Diagnostics.Where(diagnostic => diagnostic.Code == "B0300" || diagnostic.Code == "B0301" || diagnostic.Code == "A0038").ShouldBeEmpty();
            AssertDocument();
            Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V25);
        }
    }
}
