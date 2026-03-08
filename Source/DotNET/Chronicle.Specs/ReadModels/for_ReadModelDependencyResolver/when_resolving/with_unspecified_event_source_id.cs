// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDependencyResolver.when_resolving;

public class with_unspecified_event_source_id : given.a_read_model_dependency_resolver
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _resolver.Resolve(
        typeof(TestReadModel),
        new object(),
        CreateValuesWithEventSourceId(EventSourceId.Unspecified),
        _serviceProvider));

    [Fact] void should_throw_unable_to_resolve_exception() => _exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
}
