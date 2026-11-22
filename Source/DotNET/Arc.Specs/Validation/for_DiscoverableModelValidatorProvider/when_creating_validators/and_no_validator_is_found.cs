// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_DiscoverableModelValidatorProvider.when_creating_validators;

public class and_no_validator_is_found : given.a_discoverable_model_validator_provider
{
    void Establish() => _discoverableValidators.HasValidatorFor(typeof(TestCommand)).Returns(false);

    void Because() => _provider.CreateValidators(_context);

    [Fact] void should_not_add_a_validator_item() => _context.Results.ShouldBeEmpty();
}
