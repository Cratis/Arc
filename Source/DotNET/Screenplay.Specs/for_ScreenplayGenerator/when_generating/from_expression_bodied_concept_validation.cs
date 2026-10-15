// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_expression_bodied_concept_validation : a_validated_generation
{
    [Theory]
    [InlineData(false, 21, true)]
    [InlineData(true, 21, true)]
    [InlineData(false, 22, false)]
    [InlineData(true, 22, false)]
    public void should_recover_the_rule_before_deciding_generation(bool authoring, int length, bool admitted)
    {
        var registration = $"RuleFor(value => value.Value).Length({length});";
        var source = WithExpressionBody(registration);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Source.ShouldContain($"length == {length}");
        Result.Model.Concepts.Single().Validations.ShouldContainSingleItem();
        Result.Source.Contains("receipt AuthorId generated", StringComparison.Ordinal).ShouldEqual(admitted);
        if (admitted)
        {
            AssertStrategy("generate nanoid");
            Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V20);
        }
        else
        {
            Result.Source.ShouldNotContain("generate nanoid");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable && diagnostic.Message.Contains("PLAY0688", StringComparison.Ordinal)).ShouldBeTrue();
            AssertPartialDocument();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_unreadable_expression_bodied_validation(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
            (Analyzed.SlicePath, WithExpressionBody("RuleFor(value => value.Value).Must(value => value.Length == 21);")));
        Result.Source.ShouldNotContain("generate nanoid");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Model.Concepts.Single().ValidationRecoveryComplete.ShouldBeFalse();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }

    static string WithExpressionBody(string registration) => ValidatedSource("GenerateValue.NanoId()", registration)
        .Replace($"public AuthorIdValidator() {{ {registration} }}", $"public AuthorIdValidator() => {registration}", StringComparison.Ordinal);
}
