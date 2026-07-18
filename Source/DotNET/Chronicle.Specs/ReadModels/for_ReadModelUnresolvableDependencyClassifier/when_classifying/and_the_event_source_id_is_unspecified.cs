// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelUnresolvableDependencyClassifier.when_classifying;

public class and_the_event_source_id_is_unspecified : given.a_classifier
{
    IServiceProvider _serviceProvider;

    void Establish() => _serviceProvider = ServiceProviderWith(registerReadModel: true, EventSourceId.Unspecified);

    void Because() => _result = _classifier.TryClassifyAsClientInput(_parameter, _serviceProvider, out _failure);

    [Fact] void should_not_classify_as_client_input() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_a_failure() => _failure.ShouldBeNull();
}
