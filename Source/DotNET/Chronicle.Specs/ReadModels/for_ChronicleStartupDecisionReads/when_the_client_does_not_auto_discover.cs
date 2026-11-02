// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

public class when_the_client_does_not_auto_discover : Specification
{
    IChronicleClient _client;
    ServiceProvider _services;
    IDecisionReads? _reader;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.Options.Returns(new ChronicleClientOptions { EventStore = "Application", AutoDiscoverAndRegister = false });

        // Defaults that disagree with the client must not win.
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton<IOptions<ChronicleClientOptions>>(Options.Create(new ChronicleClientOptions { EventStore = "Other", AutoDiscoverAndRegister = true }))
            .AddScoped<IDecisionReads>(_ => throw new InvalidOperationException("Command reader must not be resolved"))
            .BuildServiceProvider();
    }

    async Task Because() => _reader = await new ChronicleStartupDecisionReads(_services).Get(CancellationToken.None);

    [Fact] void should_skip() => _reader.ShouldBeNull();
    [Fact] void should_not_look_up_an_event_store() => _client.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>());

    void Destroy() => _services.Dispose();
}
