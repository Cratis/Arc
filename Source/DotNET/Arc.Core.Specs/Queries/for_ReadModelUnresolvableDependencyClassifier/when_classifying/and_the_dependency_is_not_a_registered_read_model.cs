// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelUnresolvableDependencyClassifier.when_classifying;

public class and_the_dependency_is_not_a_registered_read_model : given.a_classifier
{
    IServiceProvider _serviceProvider;

    void Establish() => _serviceProvider = ServiceProviderWith(registerReadModel: false, resolvedKey: "some-key");

    void Because() => _result = _classifier.TryClassifyAsClientInput(_parameter, _serviceProvider, out _failure);

    [Fact] void should_not_classify_as_client_input() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_a_failure() => _failure.ShouldBeNull();
}
