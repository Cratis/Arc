// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

public class when_using_the_event_store : Specification
{
    IChronicleClient _client;
    ServiceProvider _services;
    ChronicleStartupDecisionReads _startup;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton<IOptions<ChronicleClientOptions>>(Options.Create(new ChronicleClientOptions { EventStore = "Startup", AutoDiscoverAndRegister = true }))
            .AddScoped<IDecisionReads>(_ => throw new InvalidOperationException("Command reader must not be resolved"))
            .BuildServiceProvider();
        _startup = new(_services);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_skip_connection_failures(bool timedOut)
    {
        Exception failure = timedOut ? new ConnectionTimedOut() : new ConnectionUnavailable("Unavailable");
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(failure));

        (await _startup.Get(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task should_honor_host_shutdown_while_waiting_for_the_store()
    {
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(new TaskCompletionSource<IEventStore>(TaskCreationOptions.RunContinuationsAsynchronously).Task);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        (await Catch.Exception(() => _startup.Get(cancellation.Token))).ShouldBeOfExactType<TaskCanceledException>();
    }

    void Destroy() => _services.Dispose();
}
