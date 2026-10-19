// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDispatch;

public class resolving_a_stream_model_without_dispatch : Specification
{
    Exception _failure;

    void Because() => _failure = Catch.Exception(() => PerStreamReadModelBinding.Resolve(typeof(UnregisteredModel), null!, null!));

    [Fact] void should_surface_the_failure_unwrapped() => _failure.ShouldBeOfExactType<MissingReadModelDispatch>();
}
