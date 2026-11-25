// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_metadata;

/// <summary>
/// Recovers the null-or-length predicates rendered for the QueryValidation V18 concepts.
/// </summary>
/// <remarks>
/// The input is the Common/CustomerCode.cs and Common/InvoiceNumber.cs shape recorded in the Stage analysis evidence
/// (l186-stage .ai-work/1195/analysis-evidence/QueryValidationCorpus.V18), not a fresh Stage render.
/// </remarks>
public class from_query_validation_v18 : a_generated_document
{
    static readonly (string Name, int Minimum, string Message)[] _concepts =
    [
        ("CustomerCode", 3, "Customer code is too short"),
        ("InvoiceNumber", 2, "Invoice number is too short")
    ];

    void Establish() => Generate((Analyzed.SlicePath, ConceptLengthSources.Concepts(_concepts.Select(concept => (concept.Name, $"value is null || value.Length >= {concept.Minimum}m", concept.Message)))));

    [Fact]
    void should_recover_exactly_the_declared_concepts() =>
        Assert.Equal(_concepts.Select(concept => concept.Name).Order(), Result.Model.Concepts.Select(concept => concept.Name).Order());

    [Fact]
    void should_recover_each_minimum_on_the_implied_value_with_its_message()
    {
        foreach (var (name, minimum, message) in _concepts)
        {
            var rule = Assert.Single(Result.Model.Concepts.Single(concept => concept.Name == name).Validations);
            rule.Kind.ShouldEqual(ValidationRuleKind.Min);
            Convert.ToInt32(rule.Value).ShouldEqual(minimum);
            rule.Message.ShouldEqual(message);
        }
    }

    [Fact]
    void should_state_the_rules_in_the_document() => Result.Source.ShouldContain("message \"Customer code is too short\"");

    [Fact]
    void should_leave_no_unmappable_validation_diagnostic() =>
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeEmpty();

    [Fact]
    void should_keep_the_document_compiling_binding_and_stable() => AssertDocument();
}
