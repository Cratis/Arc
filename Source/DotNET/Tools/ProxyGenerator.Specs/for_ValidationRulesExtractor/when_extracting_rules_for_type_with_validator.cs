// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rules_for_type_with_validator : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommand).Assembly, typeof(TestCommand));

    [Fact] void should_return_validation_descriptors() => _result.ShouldNotBeEmpty();

    [Fact] void should_have_descriptor_for_name_property() => _result.Any(_ => _.PropertyName == "name").ShouldBeTrue();

    [Fact] void should_have_descriptor_for_email_property() => _result.Any(_ => _.PropertyName == "email").ShouldBeTrue();

    [Fact] void should_have_descriptor_for_age_property() => _result.Any(_ => _.PropertyName == "age").ShouldBeTrue();

    [Fact] void should_have_descriptor_for_description_property() => _result.Any(_ => _.PropertyName == "description").ShouldBeTrue();
}
