// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelUnresolvableDependencyClassifier.when_classifying;

public class and_the_dependency_is_not_a_registered_read_model : given.a_classifier
{
    IServiceProvider _serviceProvider;

    void Establish() => _serviceProvider = ServiceProviderWith(registerReadModel: false, EventSourceId.New());

    void Because() => _result = _classifier.TryClassifyAsClientInput(_parameter, _serviceProvider, out _failure);

    [Fact] void should_not_classify_as_client_input() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_a_failure() => _failure.ShouldBeNull();
}
