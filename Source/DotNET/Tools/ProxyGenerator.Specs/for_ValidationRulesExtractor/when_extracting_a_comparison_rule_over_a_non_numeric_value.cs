// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// A GreaterThan(DateOnly.MinValue) "must be set" sentinel has no client-side comparison the browser can evaluate,
/// and its invariant text (01/01/0001) is not a TypeScript literal. It must be dropped, without taking the numeric
/// rules on the same command down with it.
/// </summary>
public class when_extracting_a_comparison_rule_over_a_non_numeric_value : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(
        typeof(TestCommandWithDateComparison).Assembly,
        typeof(TestCommandWithDateComparison));

    [Fact] void should_not_project_the_date_comparison() => _result.Any(_ => _.PropertyName == "when").ShouldBeFalse();
    [Fact] void should_still_project_the_numeric_comparison() => _result.Single(_ => _.PropertyName == "age").Rules.Single().RuleName.ShouldEqual("greaterThanOrEqual");
}
