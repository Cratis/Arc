// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.given;

public class a_host_with_an_event_source : a_chronicle_host
{
    protected AsyncServiceScope _scope;
    protected IEventStore _eventStore;
    protected IAggregateRootMutatorFactory _mutatorFactory;
    protected IAggregateRootMutator _mutator;

    [Cratis.Chronicle.EventSources.EventSource("hosted-ledger")]
    [EventStream("entries")]
    public class HostedLedger : IEventSource;

    [EventType]
    public record HostedEntryRecorded(string Description);

    [Command]
    [EventSource<HostedLedger>("entries")]
    public record RecordEntry(EventSourceId Id, string Description)
    {
        public HostedEntryRecorded Handle() => new(Description);
    }

    [EventSource<HostedLedger>("entries")]
    public class LedgerAggregate : AggregateRoot;

    void Establish()
    {
        var assemblies = Substitute.For<ICanProvideAssembliesForDiscovery>();
        assemblies.DefinedTypes.Returns([typeof(HostedLedger), typeof(HostedEntryRecorded)]);
        var artifacts = new DefaultClientArtifactsProvider(assemblies);
        _mutatorFactory = Substitute.For<IAggregateRootMutatorFactory>();
        _mutator = Substitute.For<IAggregateRootMutator>();
        _mutatorFactory.Create<LedgerAggregate>(Arg.Any<IAggregateRootContext>()).Returns(_mutator);

        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            // Substitute transport and rehydration only; use the real hosted Chronicle registration and event store.
            arc.Services.AddSingleton(Substitute.For<IChronicleConnection>());
            arc.WithChronicle(
                options =>
                {
                    options.EventStore = EventStore;
                    options.AutoDiscoverAndRegister = false;
                },
                chronicle => chronicle.WithArtifactsProvider(artifacts));
        });
        builder.Services.AddSingleton(_mutatorFactory);
        _services = builder.Services;
        _host = builder.Build();
        _scope = _host.Services.CreateAsyncScope();
        _eventStore = _scope.ServiceProvider.GetRequiredService<IEventStore>();

        // Discover locally without starting the client or registering artifacts with a Chronicle server.
        _eventStore.EventSources.Discover();
    }

    async Task Destroy() => await _scope.DisposeAsync();
}
