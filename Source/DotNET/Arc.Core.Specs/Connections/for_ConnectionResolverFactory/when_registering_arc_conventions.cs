// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.for_ConnectionResolverFactory;

public class when_registering_arc_conventions : Specification
{
    ServiceCollection _services;

    static Type[] FactoryOwnedTypes =>
    [
        typeof(CachingConnectionResolver),
        typeof(CompositeConnectionResolver),
        typeof(DefaultConnectionResolver),
        typeof(TemplateConnectionResolver),
        typeof(HttpConnectionResolver),
        typeof(ClientCredentialsAccessTokenProvider)
    ];

    void Establish() => _services = new ServiceCollection();

    void Because() => _services.AddCratisArcCore();

    [Fact] void should_include_convention_self_bindings() =>
        _services.Single(_ => _.ServiceType == typeof(ConnectionResolverFactory)).ImplementationType.ShouldEqual(typeof(ConnectionResolverFactory));

    [Fact] void should_not_bind_factory_owned_resolvers() =>
        _services.Where(_ => _.ImplementationType is { } type && FactoryOwnedTypes.Contains(type)).ShouldBeEmpty();

    [Fact] void should_not_self_bind_factory_owned_resolvers() =>
        _services.Where(_ => FactoryOwnedTypes.Contains(_.ServiceType)).ShouldBeEmpty();
}
