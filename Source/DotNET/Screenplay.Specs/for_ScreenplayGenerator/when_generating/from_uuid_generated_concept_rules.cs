// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uuid_generated_concept_rules : a_validated_generation
{
    [Theory]
    [InlineData(false, 19)]
    [InlineData(true, 19)]
    [InlineData(false, 20)]
    [InlineData(true, 20)]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    public void should_preserve_an_inadmissible_validator_without_claiming_uuid_generation(bool authoring, int cap)
    {
        var version = cap == 19 ? SemanticVersion.V19 : cap == 20 ? SemanticVersion.V20 : (SemanticVersion?)null;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = version },
            (Analyzed.SlicePath, ValidatedSource("Guid.NewGuid()", "RuleFor(value => value.Value).NotEmpty();", "Guid")));
        Result.Model.Concepts.Single().Validations.Single().Kind.ShouldEqual(ValidationRuleKind.NotEmpty);
        Result.Source.ShouldContain("rule AuthorIdValidator");
        Result.Source.ShouldContain("file Feature/Slice/Slice.cs");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("receipt = receipt");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.UnavailableGenerated.ShouldNotBeEmpty();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_an_opaque_uuid_validator_and_withhold_its_dependents(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
            (Analyzed.SlicePath, ValidatedSource("Guid.NewGuid()", "RuleFor(value => value.Value).Must(value => value != Guid.Empty);", "Guid")));
        Result.Source.ShouldContain("rule AuthorIdValidator");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("receipt = receipt");
        AssertPartialDocument();
    }
}
