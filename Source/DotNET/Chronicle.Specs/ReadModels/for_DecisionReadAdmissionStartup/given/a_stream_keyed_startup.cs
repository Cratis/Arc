// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given;

public class a_stream_keyed_startup : a_startup
{
    protected IDecisionReads _admissions;
    EventStoreForTesting _store;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(Declarations.Credited)],
            Projections = [typeof(StreamKeyedBalanceProjection)]
        };
        _store = new EventStoreForTesting(null, artifacts);
        _admissions = _store.GetDecisionReads();
        _startupReads.GetHandler = _ => Task.FromResult<IDecisionReads?>(_admissions);
    }

    void Destroy() => _store.Dispose();

    public record StreamKeyedBalance(string Id, decimal Amount);

    public class StreamKeyedBalanceProjection : IProjectionFor<StreamKeyedBalance>
    {
        public void Define(IProjectionBuilderFor<StreamKeyedBalance> builder) =>
            builder.From<Declarations.Credited>(from => from.UsingKeyFromContext(context => context.EventStreamId));
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record ProvidedStreamRead(EventSourceId Source, EventStreamType StreamType, EventStreamId StreamId)
    {
        public Task<DecisionRead<StreamKeyedBalance>> Provide(IDecisionReads reads) =>
            reads.Get<StreamKeyedBalance>(Source, StreamType, StreamId);

        public void Handle(DecisionRead<StreamKeyedBalance> balance) { }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class InjectedStreamRead
    {
        public void Handle(DecisionRead<StreamKeyedBalance> balance) { }
    }
}
