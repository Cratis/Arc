// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_min_and_max_length_rules : Specification
{
    PropertyValidationDescriptor _descriptionDescriptor;

    void Because() => _descriptionDescriptor = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommand).Assembly, typeof(TestCommand))
        .First(_ => _.PropertyName == "description");

    [Fact] void should_have_two_rules() => _descriptionDescriptor.Rules.Count().ShouldEqual(2);

    [Fact] void should_have_min_length_rule() => _descriptionDescriptor.Rules.Any(_ => _.RuleName == "minLength").ShouldBeTrue();

    [Fact] void should_have_max_length_rule() => _descriptionDescriptor.Rules.Any(_ => _.RuleName == "maxLength").ShouldBeTrue();

    [Fact] void should_have_min_length_value_of_10() => _descriptionDescriptor.Rules.First(_ => _.RuleName == "minLength").Arguments.ShouldContain(10);

    [Fact] void should_have_max_length_value_of_100() => _descriptionDescriptor.Rules.First(_ => _.RuleName == "maxLength").Arguments.ShouldContain(100);
}
