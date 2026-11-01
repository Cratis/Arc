// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.Fixtures;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.when_finding;

public class and_the_query_names_differ : Specification
{
    IDescriptor[] _descriptors;
    IReadOnlyDictionary<string, string> _map;
    IReadOnlyList<ProxyNameCollision> _collisions;

    void Establish()
    {
        _descriptors = [new FakeDescriptor(typeof(FirstReadModel), "WatchAll"), new FakeDescriptor(typeof(SecondReadModel), "WatchAllNotes")];
        _map = new Dictionary<string, string>
        {
            [typeof(FirstReadModel).FullName] = "MongoWatcher",
            [typeof(SecondReadModel).FullName] = "MongoWatcher"
        };
    }

    void Because() => _collisions = ProxyNameCollisions.Find(_descriptors, 0, _map);

    [Fact] void should_not_find_collisions() => _collisions.ShouldBeEmpty();
}
