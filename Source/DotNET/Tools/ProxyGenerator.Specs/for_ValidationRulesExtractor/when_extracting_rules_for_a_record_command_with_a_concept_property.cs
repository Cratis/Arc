// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// The shape a model-bound command actually has: a positional record whose concept property is init-only, whose
/// concept validator chains more than one rule behind lazily-resolved messages, and which carries its own command
/// validator holding a rule the client cannot express. None of that may stop the concept's rules from reaching the
/// client.
/// </summary>
/// <remarks>
/// The rules cross; the lazily-declared messages deliberately do not. A factory is deferred because its value is
/// not known at generation time, so the generator leaves it to the server rather than answering for it - see
/// <see cref="when_extracting_a_rule_whose_message_is_deferred"/> for the reasoning.
/// </remarks>
public class when_extracting_rules_for_a_record_command_with_a_concept_property : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(
        typeof(TestRecordCommandWithConcept).Assembly,
        typeof(TestRecordCommandWithConcept));

    [Fact] void should_have_rules_for_the_owning_property() => _result.Select(_ => _.PropertyName).ShouldContain("email");
    [Fact] void should_project_both_chained_concept_rules() => _result.Single(_ => _.PropertyName == "email").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty", "emailAddress");
    [Fact] void should_not_carry_a_lazily_declared_message() => _result.Single(_ => _.PropertyName == "email").Rules.Select(_ => _.ErrorMessage).ShouldContainOnly(null, null);
}
