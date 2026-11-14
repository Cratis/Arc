// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_reporting_extraction_decisions : Specification
{
    [Theory]
    [InlineData(typeof(ValidatedModel), "conditional", "conditional")]
    [InlineData(typeof(TestCommandWithDateComparison), "when", "non-numeric-comparison")]
    [InlineData(typeof(TestCommandWithSeverities), "dynamic", "dynamic-severity")]
    [InlineData(typeof(TestCommandWithNullableConcepts), "optional", "optional-concept-presence")]
    [InlineData(typeof(TypeWithDeferredMessage), "deferredMessaged", "deferred-message")]
    void should_explain_omitted_metadata_without_changing_rules(Type type, string member, string reason)
    {
        var expected = ValidationRulesExtractor.ExtractValidationRules(type.Assembly, type).ToList();
        var result = ValidationRulesExtractor.ExtractWithProjection(type.Assembly, type);
        result.Projection.Any(_ => _.Member == member && _.Reason == reason).ShouldBeTrue();
        result.Rules.Select(_ => (_.PropertyName, string.Join(',', _.Rules.Select(rule => rule.RuleName))))
            .ShouldEqual(expected.Select(_ => (_.PropertyName, string.Join(',', _.Rules.Select(rule => rule.RuleName)))));
    }

    [Fact]
    void should_report_data_annotations_and_supported_validators()
    {
        var annotations = ValidationRulesExtractor.ExtractWithProjection(typeof(AnnotatedModel).Assembly, typeof(AnnotatedModel));
        annotations.Projection.Any(_ => _.Source == "data-annotation" && _.Outcome == "projected").ShouldBeTrue();
        var validated = ValidationRulesExtractor.ExtractWithProjection(typeof(ValidatedModel).Assembly, typeof(ValidatedModel));
        validated.Projection.Any(_ => _.Member == "name" && _.RuleKind == ValidationRuleKind.Required && _.Severity == 3).ShouldBeTrue();
    }

    [Fact]
    void should_retain_the_existing_constructor_failure() => Assert.Throws<ValidationRulesCouldNotBeExtracted>(() =>
        ValidationRulesExtractor.ExtractWithProjection(typeof(TestCommandWithThrowingValidator).Assembly, typeof(TestCommandWithThrowingValidator)));
}
