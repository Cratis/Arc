// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_refused_generation_dependencies : a_validated_generation
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_prune_an_optional_mapping_without_losing_independent_production(bool authoring)
    {
        var source = ValidatedSource("GenerateValue.NanoId()", "RuleFor(value => value.Value).Length(22);")
            .Replace("AuthorId Receipt", "string? Receipt", StringComparison.Ordinal)
            .Replace("new(Name, value)", "new(Name, value.Value)", StringComparison.Ordinal);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Source.ShouldContain("name = name");
        Result.Source.ShouldNotContain("receipt =");
        Result.Source.ShouldNotContain("generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_a_required_derived_mapping_using_the_original_symbol_dependency(bool authoring)
    {
        var source = ValidatedSource("GenerateValue.NanoId()", "RuleFor(value => value.Value).Length(22);")
            .Replace("AuthorId Receipt", "string Receipt", StringComparison.Ordinal)
            .Replace("new(Name, value)", "new(Name, value.Value.ToUpperInvariant())", StringComparison.Ordinal);
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("produces");
        Result.Source.ShouldContain("handler");
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.UnavailableGenerated.ShouldNotBeEmpty();
        AssertPartialDocument();
    }
}
