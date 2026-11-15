// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_the_connection;

[Collection("UsesCurrentDirectory")]
public class without_an_explicit_connection : given.a_chronicle_host
{
    IChronicleConnection _connection;

    void Establish()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc => arc.WithChronicle(
            options => options.AutoDiscoverAndRegister = false,
            chronicle => chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>())));
        _host = builder.Build();
    }

    void Because() => _connection = _host.Services.GetService<IChronicleConnection>();

    [Fact] void should_leave_connection_creation_to_chronicles_options_factory() => _connection.ShouldBeNull();
}
