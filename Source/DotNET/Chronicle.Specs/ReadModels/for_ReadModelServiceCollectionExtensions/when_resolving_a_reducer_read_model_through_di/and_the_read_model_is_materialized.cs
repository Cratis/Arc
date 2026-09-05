// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_a_reducer_read_model_through_di;

public class and_the_read_model_is_materialized : given.registered_read_models
{
    ReducerReadModel _materialized;

    void Establish()
    {
        _materialized = new ReducerReadModel("materialized");
        _readModels.GetInstanceById(typeof(ReducerReadModel), _eventSourceId, default).Returns(Task.FromResult<object>(_materialized));
    }

    void Because() => ResolveReducerReadModel();

    [Fact] void should_resolve_the_materialized_instance() => _resolved.ShouldEqual(_materialized);
}
