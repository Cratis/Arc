// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rules_for_type_without_validator : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommandWithoutValidator).Assembly, typeof(TestCommandWithoutValidator));

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
