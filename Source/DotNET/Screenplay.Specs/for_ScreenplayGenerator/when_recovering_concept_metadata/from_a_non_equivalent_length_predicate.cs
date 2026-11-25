// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_metadata;

public class from_a_non_equivalent_length_predicate : a_generated_document
{
    [Theory]
    [InlineData("value.Length >= 3")]
    [InlineData("value is null || value.Length > 3")]
    [InlineData("value is null || value.Length <= 3")]
    [InlineData("value is not null || value.Length >= 3")]
    [InlineData("value is null && value.Length >= 3")]
    [InlineData("value is null || value.Length >= 3.5m")]
    [InlineData("value is null || value.Length >= -1")]
    [InlineData("value is null || value.Trim().Length >= 3")]
    [InlineData("value is null || value.Length >= other.Length")]
    [InlineData("other is null || value.Length >= 3")]
    [InlineData("value is null || other.Length >= 3")]
    public void should_leave_the_rule_in_code(string predicate)
    {
        var source = ConceptLengthSources.Concepts([("CustomerCode", predicate, "Customer code is too short")]);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Model.Concepts.Single().Validations.ShouldBeEmpty();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeTrue();
    }
}
