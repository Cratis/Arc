// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_validated_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("")]
    [InlineData("public AuthorIdValidator() { RuleFor(value => value.Value).NotEmpty(); }")]
    public void should_recover_generation_without_weakening_its_validator(string rules)
    {
        var source = Source("GenerateValue.Ulid()")
            .Replace("AuthorId Receipt", "AuthorId? Receipt", StringComparison.Ordinal)
            .Replace("using Cratis.Concepts;", "using Cratis.Concepts;\nusing FluentValidation;", StringComparison.Ordinal);
        source += $"\npublic class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId> {{ {rules} }}";
        Generate((Analyzed.SlicePath, source));
        AssertStrategy("generate ulid");
        if (rules.Length > 0)
        {
            Result.Source.ShouldContain("not empty");
        }
        Bound.Value!.Model.SemanticVersion.ShouldEqual(rules.Length > 0 ? SemanticVersion.V20 : SemanticVersion.V15);
    }
}
