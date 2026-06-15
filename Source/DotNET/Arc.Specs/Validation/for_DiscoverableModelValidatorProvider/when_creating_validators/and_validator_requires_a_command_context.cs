// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidatorProvider.when_creating_validators;

/// <summary>
/// A validator that depends on a command-scoped read model cannot be constructed during MVC model validation, because
/// model binding runs before the command context exists. The provider must surface an actionable error rather than
/// the opaque dependency-resolution failure.
/// </summary>
public class and_validator_requires_a_command_context : given.a_discoverable_model_validator_provider
{
    Exception _exception;

    void Establish() =>
        _discoverableValidators
            .TryGet(typeof(TestCommand), out Arg.Any<IValidator>())
            .Returns(_ => throw new NoCommandContextAvailable());

    void Because() => _exception = Catch.Exception(() => _provider.CreateValidators(_context));

    [Fact] void should_throw_read_model_validator_requires_command_pipeline() => _exception.ShouldBeOfExactType<ReadModelValidatorRequiresCommandPipeline>();
    [Fact] void should_preserve_the_underlying_cause() => _exception.InnerException.ShouldBeOfExactType<NoCommandContextAvailable>();
}
