// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDependencyResolver.when_resolving;

public class with_valid_event_source_id : given.a_read_model_dependency_resolver
{
    EventSourceId _eventSourceId;
    TestReadModel _expectedReadModel;
    Catch<object> _result;

    void Establish()
    {
        _eventSourceId = "some-event-source-id";
        _expectedReadModel = new TestReadModel { Name = "Test" };
        _readModels.GetInstanceById(typeof(TestReadModel), Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(_expectedReadModel);
    }

    void Because() => _result = _resolver.Resolve(
        typeof(TestReadModel),
        new object(),
        CreateValuesWithEventSourceId(_eventSourceId),
        _serviceProvider);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_read_model_instance()
    {
        _result.TryGetResult(out var value);
        value.ShouldEqual(_expectedReadModel);
    }
}
