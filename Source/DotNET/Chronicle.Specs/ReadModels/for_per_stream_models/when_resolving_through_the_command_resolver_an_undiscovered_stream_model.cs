// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_resolving_through_the_command_resolver_an_undiscovered_stream_model : given.a_monthly_account
{
    StreamReadModels _models;
    ServiceProvider _resolverServices;
    UndiscoveredWithdrawals _expected;
    object _result;

    void Establish()
    {
        Begin(new Withdraw(_account, "2026-10", 100), beginScope: false);
        _expected = new(_account, 250);
        _models = new(_expected);
        _resolverServices = new ServiceCollection()
            .AddSingleton(_store.EventSources)
            .AddSingleton(Substitute.For<IDecisionReads>())
            .AddSingleton<IReadModels>(_models)
            .BuildServiceProvider();
        _context = _context with { ServiceProvider = _resolverServices };
    }

    async Task Because() => _result = await new ChronicleReadModelForCommandResolver([typeof(UndiscoveredWithdrawals)]).Resolve(typeof(UndiscoveredWithdrawals), _context);

    [Fact] void should_resolve_the_model() => _result.ShouldEqual(_expected);
    [Fact] void should_use_the_command_source_and_stream() => _models.Reads.ShouldEqual([(_account.Value, "2026-10")]);
    [Fact] void should_release_the_model() => _models.Released.ShouldEqual([_expected]);

    void Destroy() => _resolverServices.Dispose();

    /// <summary>
    /// Fake read models; IReadModels.GetInstanceById for a stream is a default interface method, which Castle cannot proxy.
    /// </summary>
    /// <param name="expected">The model to return.</param>
    public class StreamReadModels(UndiscoveredWithdrawals expected) : IReadModels
    {
        public List<(string Source, string Stream)> Reads { get; } = [];
        public List<object> Released { get; } = [];

        public Task<TReadModel> GetInstanceById<TReadModel>(EventSourceId eventSourceId, EventStreamId eventStreamId)
        {
            Reads.Add((eventSourceId.Value, eventStreamId.Value));
            return Task.FromResult((TReadModel)(object)expected);
        }

        public Task<TReadModel> Release<TReadModel>(TReadModel instance)
        {
            Released.Add(instance!);
            return Task.FromResult(instance);
        }

        public Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances) => throw new NotSupportedException();
        public IMaterializedReadModels Materialized => throw new NotSupportedException();
        public Task Register() => throw new NotSupportedException();
        public Task Register<TReadModel>() => throw new NotSupportedException();
        public Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null) => throw new NotSupportedException();
        public Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null) => throw new NotSupportedException();
        public Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null) => throw new NotSupportedException();
        public ISubject<IEnumerable<TReadModel>> Observe<TReadModel>(Expression<Func<TReadModel, bool>>? filter = null) => throw new NotSupportedException();
        public Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey) => throw new NotSupportedException();
        public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>() => throw new NotSupportedException();
        public IReadModelWatcher<TReadModel> GetWatcherFor<TReadModel>() => throw new NotSupportedException();
        public Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey) => throw new NotSupportedException();
    }

    [EventStream<BankAccount>("Transactions")]
    public record UndiscoveredWithdrawals(AccountId Id, decimal Total);
}
