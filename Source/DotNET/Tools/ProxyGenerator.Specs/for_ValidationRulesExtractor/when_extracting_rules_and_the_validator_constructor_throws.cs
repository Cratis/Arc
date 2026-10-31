// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rules_and_the_validator_constructor_throws : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => ValidationRulesExtractor.ExtractValidationRules(
        typeof(TestCommandWithThrowingValidator).Assembly, typeof(TestCommandWithThrowingValidator)));

    [Fact] void should_fail_extraction() => _error.ShouldBeOfExactType<ValidationRulesCouldNotBeExtracted>();
    [Fact] void should_preserve_the_root_cause() => _error!.InnerException.ShouldBeOfExactType<InvalidOperationException>();
}
