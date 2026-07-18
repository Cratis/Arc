// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelUnresolvableDependencyClassifier.when_classifying;

public class and_the_dependency_is_a_registered_read_model_with_a_valid_id : given.a_classifier
{
    IServiceProvider _serviceProvider;

    void Establish() => _serviceProvider = ServiceProviderWith(registerReadModel: true, EventSourceId.New());

    void Because() => _result = _classifier.TryClassifyAsClientInput(_parameter, _serviceProvider, out _failure);

    [Fact] void should_classify_as_client_input() => _result.ShouldBeTrue();
    [Fact] void should_produce_a_read_model_does_not_exist_failure() => _failure.ShouldBeOfExactType<ReadModelDoesNotExistForCommand>();
    [Fact] void should_produce_a_validation_failure() => (_failure is IValidationFailure).ShouldBeTrue();
    [Fact] void should_not_leak_the_read_model_type_in_the_client_message() => ((IValidationFailure)_failure!).ValidationResult.Message.ShouldEqual("The command targets an entity that does not exist.");
}
