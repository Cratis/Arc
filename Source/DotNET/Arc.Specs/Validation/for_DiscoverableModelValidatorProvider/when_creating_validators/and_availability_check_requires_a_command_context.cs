// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidatorProvider.when_creating_validators;

public class and_availability_check_requires_a_command_context : given.a_discoverable_model_validator_provider
{
    Exception _exception;

    void Establish() => _discoverableValidators.HasValidatorFor(typeof(TestCommand)).Returns(_ => throw new NoCommandContextAvailable());

    void Because() => _exception = Catch.Exception(() => _provider.CreateValidators(_context));

    [Fact] void should_throw_read_model_validator_requires_command_pipeline() => _exception.ShouldBeOfExactType<ReadModelValidatorRequiresCommandPipeline>();
    [Fact] void should_preserve_the_underlying_cause() => _exception.InnerException.ShouldBeOfExactType<NoCommandContextAvailable>();
}
