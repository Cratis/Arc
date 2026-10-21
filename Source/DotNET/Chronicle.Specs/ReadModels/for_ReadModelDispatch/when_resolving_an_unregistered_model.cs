// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDispatch;

public class when_resolving_an_unregistered_model : Specification
{
    ReadModelDispatchEntry _entry;

    void Because() => _entry = ReadModelDispatch.For(typeof(UnregisteredModel));

    [Fact] void should_register_it_on_first_use() => _entry.ShouldNotBeNull();
    [Fact] void should_keep_that_registration() => ReadModelDispatch.For(typeof(UnregisteredModel)).ShouldBeSame(_entry);
}
