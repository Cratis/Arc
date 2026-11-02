// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_startup_decision_reads;

[Collection("UsesCurrentDirectory")]
public class and_cratis_is_added : Specification
{
    const string EventStore = "startup-decision-reads";
    WebApplication _host;
    IChronicleClient _client;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(new ConnectionUnavailable("Unavailable")));

        var builder = WebApplication.CreateBuilder();
        builder.AddCratis(configureArcBuilder: ConfigureChronicle);
        builder.Services.AddSingleton(_client);
        _host = builder.Build();
    }

    async Task Because() => await _host.Services.GetRequiredService<IStartupDecisionReads>().Get(CancellationToken.None);

    [Fact] void should_use_the_configured_event_store() => _client.Received(1).GetEventStore(Arg.Is<EventStoreName>(_ => _.Value == EventStore));
    [Fact] void should_not_use_an_unnamed_event_store() => _client.DidNotReceive().GetEventStore(Arg.Is<EventStoreName>(_ => string.IsNullOrEmpty(_.Value)));

    void Destroy() => _host?.DisposeAsync().AsTask().GetAwaiter().GetResult();

    static void ConfigureChronicle(IArcBuilder builder)
    {
        // Keep transport and artifact discovery outside this DI composition specification.
        builder.Services.AddSingleton(Substitute.For<IChronicleConnection>());
        Microsoft.AspNetCore.Builder.ArcBuilderExtensions.WithChronicle(
            builder,
            options =>
            {
                options.EventStore = EventStore;
                options.AutoDiscoverAndRegister = true;
            },
            chronicle => chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>()));
    }
}
