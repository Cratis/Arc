// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDependencyResolver.when_resolving;

public class with_unspecified_event_source_id : given.a_read_model_dependency_resolver
{
    Catch<object> _result;

    void Because() => _result = _resolver.Resolve(
        typeof(TestReadModel),
        new object(),
        CreateValuesWithEventSourceId(EventSourceId.Unspecified),
        _serviceProvider);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_unable_to_resolve_exception()
    {
        _result.TryGetException(out var exception);
        exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
    }
}
