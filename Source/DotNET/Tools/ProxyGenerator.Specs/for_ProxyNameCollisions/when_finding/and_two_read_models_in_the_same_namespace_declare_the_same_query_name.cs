// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.Fixtures;

namespace Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.when_finding;

public class and_two_read_models_in_the_same_namespace_declare_the_same_query_name : Specification
{
    FakeDescriptor _first;
    FakeDescriptor _second;
    IReadOnlyList<ProxyNameCollision> _collisions;

    void Establish()
    {
        _first = new(typeof(FirstReadModel), "WatchAll");
        _second = new(typeof(SecondReadModel), "WatchAll");
    }

    void Because() => _collisions = ProxyNameCollisions.Find([_first, _second], 0, null);

    [Fact] void should_find_one_collision() => _collisions.Count.ShouldEqual(1);
    [Fact] void should_name_the_colliding_proxy() => _collisions[0].Name.ShouldEqual("WatchAll");
    [Fact] void should_include_both_descriptors() => _collisions[0].Descriptors.ShouldContainOnly(_first, _second);
}
