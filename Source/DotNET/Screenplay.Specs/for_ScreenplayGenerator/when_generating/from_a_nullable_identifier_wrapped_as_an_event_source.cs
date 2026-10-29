// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_nullable_identifier_wrapped_as_an_event_source : a_generated_document
{
    [Theory]
    [InlineData("string?", "new EventSourceId(Id!)")]
    [InlineData("string?", "new EventSourceId((Id ?? OtherId)!)")]
    [InlineData("Guid?", "new EventSourceId(Id.ToString()!)")]
    public void should_not_turn_optional_inputs_into_required_identifiers(string type, string expression)
    {
        Generate((Analyzed.SlicePath, GeneratedIdentifierSources.With(type, expression, expression)));
        Result.Source.ShouldNotContain(" identifier");
        var diagnostic = Result.Diagnostics.Single(item => item.Code == ScreenplayDiagnosticCodes.UnreadableCommandIdentifier);
        diagnostic.Location.ShouldNotBeNull();
        Result.Diagnostics.Any(item => item.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        AssertPartialDocument();
    }
}
