// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_identifier_with_custom_formatting : a_generated_document
{
    [Theory]
    [InlineData("Guid", "new EventSourceId(Id.ToString(\"N\"))")]
    [InlineData("long", "new EventSourceId(Id.ToString(\"X\", CultureInfo.InvariantCulture))")]
    [InlineData("long", "new EventSourceId(Id.ToString(CultureInfo.CurrentCulture))")]
    [InlineData("CustomIdentity", "Id")]
    [InlineData("CustomIdentity", "new EventSourceId(Id.ToString())")]
    public void should_reject_unproven_text_and_conversions(string type, string expression)
    {
        const string Declaration = """
            public record CustomIdentity(string Value)
            {
                public override string ToString() => "changed";
                public static implicit operator EventSourceId(CustomIdentity value) => new("changed");
            }
            """;
        var source = GeneratedIdentifierSources.With(type, expression, expression).Replace(
            "namespace Library.Authors.Registration;", "namespace Library.Authors.Registration;" + Declaration, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain(" identifier");
        var diagnostic = Result.Diagnostics.Single(item => item.Code == ScreenplayDiagnosticCodes.UnreadableCommandIdentifier);
        diagnostic.Location.ShouldNotBeNull();
        Result.Diagnostics.Any(item => item.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldBeTrue();
        AssertPartialDocument();
    }
}
