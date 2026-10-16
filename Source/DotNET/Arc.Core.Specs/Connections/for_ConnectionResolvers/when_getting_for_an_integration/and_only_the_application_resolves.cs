// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolvers.when_getting_for_an_integration;

public class and_only_the_application_resolves : given.connection_resolvers
{
    ICachingConnectionResolver? _mongoDB;
    ICachingConnectionResolver? _entityFrameworkCore;

    void Establish() => _arcOptions.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));

    void Because()
    {
        _mongoDB = _resolvers.For(new ConnectionResolutionOptions());
        _entityFrameworkCore = _resolvers.For(new ConnectionResolutionOptions());
    }

    [Fact] void should_resolve() => _mongoDB.ShouldNotBeNull();
    [Fact] void should_share_one_resolver_between_integrations() => _mongoDB.ShouldBeSame(_entityFrameworkCore);
    [Fact] void should_create_it_from_the_application_options() => _factory.Received(1).Create(_arcOptions.ConnectionResolution);
}
