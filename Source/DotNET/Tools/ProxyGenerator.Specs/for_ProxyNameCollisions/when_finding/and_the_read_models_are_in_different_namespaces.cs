// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.Fixtures;
using Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.Fixtures.Other;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.when_finding;

public class and_the_read_models_are_in_different_namespaces : Specification
{
    IDescriptor[] _descriptors;
    IReadOnlyList<ProxyNameCollision> _collisions;

    void Establish() => _descriptors = [new FakeDescriptor(typeof(FirstReadModel), "WatchAll"), new FakeDescriptor(typeof(OtherReadModel), "WatchAll")];

    void Because() => _collisions = ProxyNameCollisions.Find(_descriptors, 0, null);

    [Fact] void should_not_find_collisions() => _collisions.ShouldBeEmpty();
}
