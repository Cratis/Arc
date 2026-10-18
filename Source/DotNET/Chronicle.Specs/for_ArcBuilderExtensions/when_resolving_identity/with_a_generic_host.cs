// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Identities;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_identity;

[Collection("UsesCurrentDirectory")]
public class with_a_generic_host : given.a_chronicle_host
{
    ChronicleIdentity _identity;
    IChronicleBuilder _chronicle;

    void Establish()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.Services.AddSingleton(Substitute.For<Cratis.Chronicle.Connections.IChronicleConnection>());
            arc.WithChronicle(
                options =>
                {
                    options.EventStore = EventStore;
                    options.AutoDiscoverAndRegister = false;
                },
                chronicle =>
                {
                    _chronicle = chronicle;
                    chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>());
                });
        });
        _host = builder.Build();
    }

    void Because()
    {
        _host.Services.GetRequiredService<IChronicleClient>();
        var provider = _host.Services.GetRequiredService<ArcIdentityProvider>();
        using var scope = _host.Services.GetRequiredService<ISystemExecution>().As(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "worker")], "test")));
        _identity = provider.GetCurrent();
    }

    [Fact] void should_install_arcs_provider_in_the_chronicle_builder() => _chronicle.IdentityProvider.ShouldBeSame(_host.Services.GetRequiredService<ArcIdentityProvider>());
    [Fact] void should_resolve_the_scoped_principal() => _identity.ShouldEqual(new ChronicleIdentity("worker", string.Empty));
}
