// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

public class when_the_event_store_is_not_resolved : Specification
{
    IChronicleClient _client;
    ServiceProvider _services;
    ChronicleStartupDecisionReads _startup;
    Cratis.Chronicle.ReadModels.IDecisionReads? _result;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.Options.Returns(new ChronicleOptions());
        _services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(_client)
            .BuildServiceProvider();
        _startup = new(_services, _ => new ChronicleClientOptions { AutoDiscoverAndRegister = true });
    }

    async Task Because() => _result = await _startup.Get(CancellationToken.None);

    [Fact] void should_not_ask_the_client_for_an_event_store() => _client.DidNotReceiveWithAnyArgs().GetEventStore(default!);
    [Fact] void should_not_provide_decision_reads() => _result.ShouldBeNull();

    void Destroy() => _services.Dispose();
}
