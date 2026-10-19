// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDispatch;

public class registering_twice : Specification
{
    ReadModelDispatchEntry _first;
    ReadModelDispatchEntry _second;

    void Because()
    {
        ReadModelDispatch.Register<DispatchedModel>();
        _first = ReadModelDispatch.For(typeof(DispatchedModel));
        ReadModelDispatch.Register<DispatchedModel>();
        _second = ReadModelDispatch.For(typeof(DispatchedModel));
    }

    [Fact] void should_keep_the_first_registration() => _second.ShouldBeSame(_first);
}
