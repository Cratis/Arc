// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_validated_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("")]
    [InlineData("public AuthorIdValidator() { RuleFor(value => value.Value).NotEmpty(); }")]
    public void should_keep_validator_dependent_generation_in_code(string rules)
    {
        var source = Source("GenerateValue.Ulid()")
            .Replace("AuthorId Receipt", "AuthorId? Receipt", StringComparison.Ordinal)
            .Replace("using Cratis.Concepts;", "using Cratis.Concepts;\nusing FluentValidation;", StringComparison.Ordinal);
        source += $"\npublic class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId> {{ {rules} }}";
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain(" generated");
        Result.Source.ShouldNotContain("receipt = receipt");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("concept validators");
        if (rules.Length > 0)
        {
            Result.Source.ShouldNotContain("generate ulid");
            Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).Message.ShouldContain("validation rules");
        }
        AssertDocument();
    }
}
