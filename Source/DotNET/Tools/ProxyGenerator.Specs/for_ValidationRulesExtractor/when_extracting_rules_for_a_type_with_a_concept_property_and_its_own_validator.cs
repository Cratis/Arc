// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// Both validators run server-side, so the client has to apply both to agree with it — a concept's rules add to the
/// model's own rather than replacing them.
/// </summary>
public class when_extracting_rules_for_a_type_with_a_concept_property_and_its_own_validator : Specification
{
    PropertyValidationDescriptor _emailDescriptor;

    void Because() => _emailDescriptor = ValidationRulesExtractor.ExtractValidationRules(
            typeof(TestCommandWithConceptAndOwnValidator).Assembly,
            typeof(TestCommandWithConceptAndOwnValidator))
        .Single(_ => _.PropertyName == "email");

    [Fact] void should_have_both_rules() => _emailDescriptor.Rules.Count().ShouldEqual(2);
    [Fact] void should_keep_the_rule_from_the_models_own_validator() => _emailDescriptor.Rules.Select(_ => _.RuleName).ShouldContain("notEmpty");
    [Fact] void should_add_the_rule_from_the_concepts_validator() => _emailDescriptor.Rules.Select(_ => _.RuleName).ShouldContain("emailAddress");
}
