// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_startup_decision_reads.given;

public class a_cratis_web_host : Specification
{
    protected const string EventStore = "startup-decision-reads";
    protected WebApplication _host;
    protected IChronicleClient _client;
    protected IDecisionReads? _reads;

    protected void Build(Action<WebApplicationBuilder>? configureHost = default, Action<ChronicleAspNetCoreOptions>? configureOptions = default)
    {
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(new ConnectionUnavailable("Unavailable")));

        var builder = WebApplication.CreateBuilder();
        configureHost?.Invoke(builder);
        builder.AddCratis(configureArcBuilder: arcBuilder =>
        {
            // Keep transport and artifact discovery outside this DI composition specification.
            arcBuilder.Services.AddSingleton(Substitute.For<IChronicleConnection>());
            arcBuilder.WithChronicle(
                configureOptions,
                chronicle => chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>()));
        });
        builder.Services.AddSingleton(_client);
        _host = builder.Build();
    }

    protected async Task ResolveStartupReads() => _reads = await _host.Services.GetRequiredService<IStartupDecisionReads>().Get(CancellationToken.None);

    void Destroy() => _host?.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
