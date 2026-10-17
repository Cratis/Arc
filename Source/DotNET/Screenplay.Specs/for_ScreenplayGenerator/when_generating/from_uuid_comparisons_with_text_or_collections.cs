// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uuid_comparisons_with_text_or_collections : a_generated_document
{
    [Theory]
    [InlineData("Member == Text")]
    [InlineData("Member != Owners")]
    public void should_refuse_operands_that_are_not_scalar_uuids(string comparison)
    {
        const string replacement = """
            public record MemberId(Guid Value) : ConceptAs<Guid>(Value)
            {
                public static bool operator ==(MemberId left, string right) => false;
                public static bool operator !=(MemberId left, string right) => true;
                public static bool operator ==(MemberId left, Guid[] right) => false;
                public static bool operator !=(MemberId left, Guid[] right) => true;
            }
            """;

        var source = UuidComparisonSources.Guard(comparison, "public Guid[] Owners { get; init; } = [];")
            .Replace("public record MemberId(Guid Value) : ConceptAs<Guid>(Value);", replacement, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("when member");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("a Uuid cannot be compared with a value of another type", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
