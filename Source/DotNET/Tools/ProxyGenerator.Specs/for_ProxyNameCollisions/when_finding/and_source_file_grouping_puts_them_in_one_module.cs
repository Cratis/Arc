// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.Fixtures;

namespace Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.when_finding;

public class and_source_file_grouping_puts_them_in_one_module : Specification
{
    FakeDescriptor _first;
    FakeDescriptor _second;
    FakeDescriptor _distinct;
    IReadOnlyDictionary<string, string> _map;
    IReadOnlyList<ProxyNameCollision> _collisions;

    void Establish()
    {
        _first = new(typeof(FirstReadModel), "WatchAll");
        _second = new(typeof(SecondReadModel), "WatchAll");
        _distinct = new(typeof(FirstReadModel), "ObserveAll");
        _map = new Dictionary<string, string>
        {
            [typeof(FirstReadModel).FullName] = "MongoWatcher",
            [typeof(SecondReadModel).FullName] = "MongoWatcher"
        };
    }

    void Because() => _collisions = ProxyNameCollisions.Find([_first, _second, _distinct], 0, _map);

    [Fact] void should_find_one_collision() => _collisions.Count.ShouldEqual(1);
    [Fact] void should_name_the_colliding_proxy() => _collisions[0].Name.ShouldEqual("WatchAll");
    [Fact] void should_include_both_colliding_descriptors() => _collisions[0].Descriptors.ShouldContainOnly(_first, _second);
    [Fact] void should_not_include_the_distinct_proxy() => _collisions[0].Descriptors.ShouldNotContain(_distinct);
}
