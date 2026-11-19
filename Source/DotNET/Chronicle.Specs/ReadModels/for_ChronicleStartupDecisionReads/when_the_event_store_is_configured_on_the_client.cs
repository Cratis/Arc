// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

public class when_the_event_store_is_configured_on_the_client : Specification
{
    IChronicleClient _client;
    ServiceProvider _services;
    ChronicleStartupDecisionReads _startup;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.Options.Returns(new ChronicleClientOptions { EventStore = "Host", AutoDiscoverAndRegister = true });
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(new ConnectionUnavailable("Unavailable")));
        _services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(_client)
            .BuildServiceProvider();
        _startup = new(_services, _ => new ChronicleClientOptions { AutoDiscoverAndRegister = true });
    }

    async Task Because() => await _startup.Get(CancellationToken.None);

    [Fact] void should_use_the_event_store_the_client_was_configured_with() => _client.Received(1).GetEventStore(Arg.Is<EventStoreName>(_ => _.Value == "Host"));

    void Destroy() => _services.Dispose();
}
