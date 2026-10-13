// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_parameter_that_is_not_required : a_generated_document
{
    [Theory]
    [InlineData("AuthorId?")]
    [InlineData("ref AuthorId")]
    public void should_leave_generation_in_code_and_explain_the_required_scalar_contract(string parameterType)
    {
        Generate((Analyzed.SlicePath, GeneratedValueSources.With($$"""
            [Command] public record RegisterAuthor(string Name)
            {
                public AuthorRegistered Handle([GeneratedValue] {{parameterType}} authorId) => new(Name);
            }
            """)));
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable)
            .Message.ShouldContain("not a required scalar generatable concept");
        AssertDocument();
    }
}
