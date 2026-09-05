// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelUnresolvableDependencyClassifier.when_classifying;

public class and_the_resolved_key_is_empty : given.a_classifier
{
    IServiceProvider _serviceProvider;

    void Establish() => _serviceProvider = ServiceProviderWith(registerReadModel: true, resolvedKey: string.Empty);

    void Because() => _result = _classifier.TryClassifyAsClientInput(_parameter, _serviceProvider, out _failure);

    [Fact] void should_not_classify_as_client_input() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_a_failure() => _failure.ShouldBeNull();
}
