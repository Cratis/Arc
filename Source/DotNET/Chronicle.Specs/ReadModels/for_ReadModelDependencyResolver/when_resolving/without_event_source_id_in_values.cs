// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDependencyResolver.when_resolving;

public class without_event_source_id_in_values : given.a_read_model_dependency_resolver
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _resolver.Resolve(
        typeof(TestReadModel),
        new object(),
        new CommandContextValues(),
        _serviceProvider));

    [Fact] void should_throw_unable_to_resolve_exception() => _exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
}
