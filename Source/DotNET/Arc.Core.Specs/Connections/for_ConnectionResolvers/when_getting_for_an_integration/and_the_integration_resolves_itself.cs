// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolvers.when_getting_for_an_integration;

public class and_the_integration_resolves_itself : given.connection_resolvers
{
    ConnectionResolutionOptions _integration;
    ICachingConnectionResolver? _first;
    ICachingConnectionResolver? _second;
    ICachingConnectionResolver? _application;

    void Establish()
    {
        _arcOptions.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
        _integration = new();
        _integration.Connection.UseTemplate(database: "tenant-{tenant}");
    }

    void Because()
    {
        _first = _resolvers.For(_integration);
        _second = _resolvers.For(_integration);
        _application = _resolvers.For(new ConnectionResolutionOptions());
    }

    [Fact] void should_create_it_from_the_integration_options() => _factory.Received(1).Create(_integration);
    [Fact] void should_keep_one_resolver_for_the_integration() => _first.ShouldBeSame(_second);
    [Fact] void should_not_use_the_application_resolver() => _first.ShouldNotBeSame(_application);
}
