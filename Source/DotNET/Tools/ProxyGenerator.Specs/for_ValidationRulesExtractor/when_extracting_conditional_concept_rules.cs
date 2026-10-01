// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_conditional_concept_rules : Specification
{
    IReadOnlyList<ValidationRuleDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractRulesForConceptType(
        typeof(ConditionalName).Assembly,
        typeof(ConditionalName));

    [Fact] void should_omit_rules_with_conditions_that_the_client_cannot_express() => _result.Select(_ => _.RuleName).ShouldContainOnly("maxLength", "minLength");
    [Fact] void should_keep_unconditional_components_in_a_partly_conditional_rule() => _result.Single(_ => _.RuleName == "minLength").Arguments.Single().ShouldEqual(1);
}
