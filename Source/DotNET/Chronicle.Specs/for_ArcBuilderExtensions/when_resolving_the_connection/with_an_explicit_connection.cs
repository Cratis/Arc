// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_the_connection;

[Collection("UsesCurrentDirectory")]
public class with_an_explicit_connection : given.a_chronicle_host
{
    IChronicleConnection _explicitConnection;
    IChronicleConnection _connection;

    void Establish()
    {
        _explicitConnection = Substitute.For<IChronicleConnection>();
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.Services.AddSingleton(_explicitConnection);
            arc.WithChronicle(options => options.AutoDiscoverAndRegister = false);
        });
        _host = builder.Build();
    }

    void Because() => _connection = _host.Services.GetRequiredService<IChronicleConnection>();

    [Fact] void should_preserve_the_application_connection() => _connection.ShouldBeSame(_explicitConnection);
}
