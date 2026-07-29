// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_read_model;

public class without_event_source_id : given.a_read_model_resolution
{
    void Establish() => GivenEventSourceIdIsUnspecified();

    void Because() => CatchResolveReadModelException();

    [Fact] void should_throw_unable_to_resolve_read_model_from_command_context() => _exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
    [Fact] void should_be_a_validation_failure() => (_exception is IValidationFailure).ShouldBeTrue();
    [Fact] void should_carry_a_validation_error() => ((IValidationFailure)_exception).ValidationResult.Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_not_leak_the_read_model_type_in_the_client_message() => ((IValidationFailure)_exception).ValidationResult.Message.ShouldEqual("The command is missing the identifier required to load its current state.");
    [Fact] void should_not_release_the_read_model() => ShouldNotHaveReleasedReadModel();
}
