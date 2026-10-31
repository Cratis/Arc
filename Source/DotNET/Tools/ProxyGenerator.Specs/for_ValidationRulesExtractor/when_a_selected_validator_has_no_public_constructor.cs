// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_a_selected_validator_has_no_public_constructor : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => _ = ValidationRulesExtractor.ExtractValidationRules(
        typeof(given.model_without_constructible_validator).Assembly,
        typeof(given.model_without_constructible_validator)).ToArray());

    [Fact] void should_fail_instead_of_extracting_empty_rules() => _error.ShouldBeOfExactType<ValidatorRuntimeCouldNotBeLoaded>();
    [Fact] void should_identify_the_model() => _error!.Message.ShouldContain(nameof(given.model_without_constructible_validator));
    [Fact] void should_identify_the_validator() => _error!.Message.ShouldContain(nameof(given.validator_without_public_constructor));
    [Fact] void should_retain_the_constructor_failure() => _error!.GetBaseException().ShouldBeOfExactType<MissingMethodException>();
}
