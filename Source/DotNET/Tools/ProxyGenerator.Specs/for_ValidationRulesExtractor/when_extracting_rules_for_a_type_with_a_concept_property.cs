// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// The type has no validator of its own — only the concept it carries does. The concept's rules must still reach the
/// client, otherwise the browser enforces less than the server does for the very same value.
/// </summary>
public class when_extracting_rules_for_a_type_with_a_concept_property : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(
        typeof(TestCommandWithConcept).Assembly,
        typeof(TestCommandWithConcept));

    [Fact] void should_have_rules_for_the_owning_property() => _result.Select(_ => _.PropertyName).ShouldContain("email");
    [Fact] void should_attribute_the_concept_rule_to_the_owning_property() => _result.Single(_ => _.PropertyName == "email").Rules.Single().RuleName.ShouldEqual("emailAddress");
    [Fact] void should_carry_the_custom_message() => _result.Single(_ => _.PropertyName == "email").Rules.Single().ErrorMessage.ShouldEqual(EmailAddressValidator.InvalidMessage);
}
