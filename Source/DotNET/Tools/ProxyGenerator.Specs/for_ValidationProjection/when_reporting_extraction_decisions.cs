// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_reporting_extraction_decisions : Specification
{
    [Theory]
    [InlineData(typeof(ValidatedModel), "conditional", "server-only-chained-condition")]
    [InlineData(typeof(ProjectionConceptModel), "name", "concept-condition-server-only")]
    [InlineData(typeof(ProjectionConceptModel), "name", "concept-contribution-projected")]
    [InlineData(typeof(TestCommandWithDateComparison), "when", "unsupported-validator")]
    [InlineData(typeof(TestCommandWithSeverities), "dynamic", "projected")]
    [InlineData(typeof(TestCommandWithNullableConcepts), "optional", "optional-concept-presence")]
    [InlineData(typeof(TypeWithDeferredMessage), "deferredMessaged", "projected")]
    void should_explain_metadata_without_changing_rules(Type type, string member, string reason)
    {
        var expected = ValidationRulesExtractor.ExtractValidationRules(type.Assembly, type).ToList();
        var result = ProjectionExtraction.For(type);
        result.Records.Any(_ => _.Member == member && _.Reason == reason).ShouldBeTrue();
        result.Rules.Select(_ => (_.PropertyName, string.Join(',', _.Rules.Select(rule => rule.RuleName))))
            .ShouldEqual(expected.Select(_ => (_.PropertyName, string.Join(',', _.Rules.Select(rule => rule.RuleName)))));
    }

    [Fact]
    void should_report_data_annotations_and_supported_validators()
    {
        var annotations = ProjectionExtraction.For(typeof(AnnotatedModel));
        annotations.Records.Any(_ => _.Source == ValidationProjectionSource.DataAnnotation && _.Outcome == "projected").ShouldBeTrue();
        var validated = ProjectionExtraction.For(typeof(ValidatedModel));
        validated.Records.Any(_ => _.Member == "name" && _.RuleKind == ValidationRuleKind.Required && _.Severity == 3).ShouldBeTrue();
    }

    [Fact]
    void should_retain_the_existing_constructor_failure() => Assert.Throws<ValidationRulesCouldNotBeExtracted>(() =>
        ProjectionExtraction.For(typeof(TestCommandWithThrowingValidator)));
}
