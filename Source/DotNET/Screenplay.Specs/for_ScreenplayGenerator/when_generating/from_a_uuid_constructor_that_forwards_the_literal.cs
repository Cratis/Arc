// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_uuid_constructor_that_forwards_the_literal : a_generated_document
{
    void Because()
    {
        var replacement = """
            public record MemberId : ConceptAs<Guid>
            {
                public MemberId(Guid value) : base(value) { }
            }
            """;

        var source = UuidComparisonSources.Guard("Member == new MemberId(Guid.Parse(\"0f8fad5b-d9cb-469f-a165-70867728950e\"))")
            .Replace("public record MemberId(Guid Value) : ConceptAs<Guid>(Value);", replacement, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
    }

    [Fact] void should_emit_the_unchanged_literal() => Result.Source.ShouldContain("when member == \"0f8fad5b-d9cb-469f-a165-70867728950e\"");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_require_v28() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
}
