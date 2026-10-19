// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDispatch;

public class resolving_an_unregistered_model : Specification
{
    Exception _failure;

    void Because() => _failure = Catch.Exception(() => ReadModelDispatch.For(typeof(UnregisteredModel)));

    [Fact] void should_throw_missing_read_model_dispatch() => _failure.ShouldBeOfExactType<MissingReadModelDispatch>();
    [Fact] void should_name_the_model() => _failure.Message.ShouldContain(nameof(UnregisteredModel));
}
