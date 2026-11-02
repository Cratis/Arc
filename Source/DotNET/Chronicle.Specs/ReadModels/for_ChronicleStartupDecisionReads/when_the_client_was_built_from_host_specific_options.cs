// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

/// <summary>
/// An ASP.NET Core host builds its client from options derived from <see cref="ChronicleClientOptions"/> and leaves
/// <c>IOptions&lt;ChronicleClientOptions&gt;</c> unconfigured, so it resolves to defaults without an event store.
/// </summary>
public class when_the_client_was_built_from_host_specific_options : Specification
{
    IChronicleClient _client;
    ServiceProvider _services;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.Options.Returns(new HostSpecificOptions { EventStore = "Application", AutoDiscoverAndRegister = true });

        // The startup only looks the store up here; reaching the decision reads needs a real event store.
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(new ConnectionUnavailable("Unavailable")));
        _services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(_client)
            .AddScoped<IDecisionReads>(_ => throw new InvalidOperationException("Command reader must not be resolved"))
            .BuildServiceProvider();
    }

    async Task Because() => await new ChronicleStartupDecisionReads(_services).Get(CancellationToken.None);

    [Fact] void should_use_the_event_store_the_client_was_built_with() => _client.Received(1).GetEventStore("Application");
    [Fact] void should_not_use_an_unnamed_event_store() => _client.DidNotReceive().GetEventStore(Arg.Is<EventStoreName>(name => string.IsNullOrEmpty(name.Value)));

    void Destroy() => _services.Dispose();

    sealed class HostSpecificOptions : ChronicleClientOptions;
}
