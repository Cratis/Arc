// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uuid_constructors_that_transform_the_literal : a_generated_document
{
    [Theory]
    [InlineData("Guid.Empty")]
    [InlineData("Guid.NewGuid()")]
    public void should_refuse_an_unproven_concept_literal(string forwarded)
    {
        var replacement = $$"""
            public record MemberId : ConceptAs<Guid>
            {
                public MemberId(Guid value) : base({{forwarded}}) { }
            }
            """;

        var source = UuidComparisonSources.Guard("Member == new MemberId(Guid.Parse(\"0f8fad5b-d9cb-469f-a165-70867728950e\"))")
            .Replace("public record MemberId(Guid Value) : ConceptAs<Guid>(Value);", replacement, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("when member");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("neither a canonical Uuid literal", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
