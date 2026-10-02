// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_concept_rules_for_optional_members : Specification
{
    IReadOnlyList<ValidationRuleDescriptor> _stringRules;
    IReadOnlyList<ValidationRuleDescriptor> _numberRules;

    void Because()
    {
        _stringRules = ValidationRulesExtractor.ExtractRulesForConceptType(typeof(ConstrainedText).Assembly, typeof(ConstrainedText), isOptional: true);
        _numberRules = ValidationRulesExtractor.ExtractRulesForConceptType(typeof(ConstrainedNumber).Assembly, typeof(ConstrainedNumber), isOptional: true);
    }

    [Fact] void should_omit_not_null() => _stringRules.Select(_ => _.RuleName).ShouldNotContain("notNull");
    [Fact] void should_omit_not_empty() => _stringRules.Select(_ => _.RuleName).ShouldNotContain("notEmpty");
    [Fact] void should_keep_null_tolerant_string_rules() => _stringRules.Select(_ => _.RuleName).ShouldContainOnly("minLength", "maxLength", "length", "length", "emailAddress", "matches");
    [Fact] void should_keep_null_tolerant_numeric_rules() => _numberRules.Select(_ => _.RuleName).ShouldContainOnly("greaterThan", "greaterThanOrEqual", "lessThan", "lessThanOrEqual");
    [Fact] void should_preserve_arguments() => _stringRules.Single(_ => _.RuleName == "maxLength").Arguments.Single().ShouldEqual(10);
    [Fact] void should_preserve_the_message() => _stringRules.Single(_ => _.RuleName == "maxLength").ErrorMessage.ShouldEqual("At most ten characters");
    [Fact] void should_preserve_severity() => _stringRules.Single(_ => _.RuleName == "maxLength").Severity.ShouldEqual(2);
}
