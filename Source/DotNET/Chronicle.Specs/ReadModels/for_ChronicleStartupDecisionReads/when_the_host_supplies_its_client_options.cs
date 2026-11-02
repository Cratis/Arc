// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

public class when_the_host_supplies_its_client_options : Specification
{
    IChronicleClient _client;
    ServiceProvider _services;
    ChronicleStartupDecisionReads _startup;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(new ConnectionUnavailable("Unavailable")));

        // An unconfigured ChronicleClientOptions resolves to an unnamed event store; the supplied options must win.
        _services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(_client)
            .BuildServiceProvider();
        _startup = new(_services, _ => new ChronicleClientOptions { EventStore = "Host", AutoDiscoverAndRegister = true });
    }

    async Task Because() => await _startup.Get(CancellationToken.None);

    [Fact] void should_use_the_supplied_event_store() => _client.Received(1).GetEventStore(Arg.Is<EventStoreName>(_ => _.Value == "Host"));

    void Destroy() => _services.Dispose();
}
