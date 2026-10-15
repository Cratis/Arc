// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Connections.for_ConnectionResolverFactory.when_creating;

public class with_a_custom_resolver : Specification
{
    ConnectionResolutionOptions _options;
    ResolvedConnection _result;

    void Establish()
    {
        _options = new();
        _options.Connection.UseTemplate(database: "app-{tenant}");
        _options.Identity.UseCustom<IdentityFromService>();
    }

    async Task Because()
    {
        var services = new ServiceCollection().AddSingleton(new IdentitySource("platform-user"));
        var resolver = new ConnectionResolverFactory(services.BuildServiceProvider()).Create(_options);
        _result = await resolver.Resolve(new(WellKnownConnectionStorage.PostgreSql, ConnectionPurpose.Runtime, new Dictionary<string, string> { ["tenant"] = "acme" }));
    }

    [Fact] void should_resolve_the_database_from_the_connection_slot() => _result.Database.ShouldEqual("app-acme");
    [Fact] void should_resolve_the_identity_from_the_custom_resolver_with_its_dependencies() => _result.Identity!.UserName.ShouldEqual("platform-user");

    public record IdentitySource(string UserName);

    public class IdentityFromService(IdentitySource source) : IConnectionResolver
    {
        public Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResolvedConnection(Identity: new(source.UserName, "password")));
    }
}
