// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB.for_MongoDBConnectionResolutionHooks;

public class when_registering_arc_conventions : Specification
{
    ServiceCollection _services;

    void Establish() => _services = new ServiceCollection();

    void Because()
    {
        // The spec assembly holds deliberately invalid derived types for other specs, so register only Arc's
        // convention bindings instead of everything AddCratisArcCore sets up.
        _services.AddTypeDiscovery().AddArcServiceBindings();
    }

    [Fact] void should_include_convention_self_bindings() =>
        _services.Single(_ => _.ServiceType == typeof(MongoDBConnections)).ImplementationType.ShouldEqual(typeof(MongoDBConnections));

    [Fact] void should_not_bind_per_request_hooks() =>
        _services.Where(_ => _.ImplementationType == typeof(MongoDBConnectionResolutionHooks)).ShouldBeEmpty();

    [Fact] void should_not_self_bind_per_request_hooks() =>
        _services.Where(_ => _.ServiceType == typeof(MongoDBConnectionResolutionHooks)).ShouldBeEmpty();

    [Fact] void should_not_self_bind_resolved_connections() =>
        _services.Where(_ => _.ServiceType == typeof(MongoDBConnection)).ShouldBeEmpty();
}
