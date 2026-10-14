// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_a_self_reducing_model;

public class and_the_model_exists : given.registered_self_model
{
    SelfReducedReadModel _stored;
    SelfReducedReadModel _released;

    void Establish()
    {
        _stored = new(1);
        _released = new(2);
        _readModels.GetInstanceById(typeof(SelfReducedReadModel), _id).Returns(Task.FromResult<object>(_stored));
        _readModels.Release(_stored).Returns(Task.FromResult(_released));
    }

    void Because() => Resolve();

    [Fact] void should_return_the_released_instance() => _resolved.ShouldBeSame(_released);
    [Fact] void should_read_through_chronicle_by_the_command_key() => _readModels.Received(1).GetInstanceById(Arg.Is<Type>(_ => _ == typeof(SelfReducedReadModel)), _id);
    [Fact] void should_release_protected_values() => _readModels.Received(1).Release(_stored);
}
