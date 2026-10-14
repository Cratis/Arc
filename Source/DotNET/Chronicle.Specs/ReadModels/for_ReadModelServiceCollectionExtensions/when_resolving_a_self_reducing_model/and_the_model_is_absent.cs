// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_a_self_reducing_model;

public class and_the_model_is_absent : given.registered_self_model
{
    void Establish() => _readModels.GetInstanceById(typeof(SelfReducedReadModel), _id).Returns(Task.FromResult<object>(null!));

    void Because() => Resolve();

    [Fact] void should_resolve_to_null() => _resolved.ShouldBeNull();
    [Fact] void should_not_release_an_absent_model() => _readModels.DidNotReceive().Release(Arg.Any<SelfReducedReadModel>());
}
