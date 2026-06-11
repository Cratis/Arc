// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Queries;

/// <summary>
/// Read model for query health monitoring.
/// </summary>
[ReadModel]
[Route("/.cratis/queries/health")]
[AllowAnonymous]
public sealed record QueryHealth
{
    /// <summary>
    /// Gets the connection health information.
    /// </summary>
    public required IEnumerable<QueryConnectionHealth> Connections { get; init; }

    /// <summary>
    /// Gets the total number of active connections.
    /// </summary>
    public int TotalConnections => Connections.Count();

    /// <summary>
    /// Gets the total number of active subscriptions across all connections.
    /// </summary>
    public int TotalSubscriptions => Connections.Sum(c => c.Subscriptions.Count);

    /// <summary>
    /// Gets a query-centric view of all active subscriptions, grouped by query name.
    /// </summary>
    /// <remarks>
    /// Each entry's <c>QueryName</c> matches the <c>queryName</c> property on the generated
    /// TypeScript proxy, enabling cross-stack correlation between frontend cache diagnostics
    /// and backend subscription state across all transport modes (multiplexed WebSocket and
    /// direct SSE).
    /// </remarks>
    public IEnumerable<QuerySubscriptionAggregate> QuerySubscriptions =>
        Connections
            .SelectMany(c => c.Subscriptions.Select(s => (Connection: c, Subscription: s)))
            .GroupBy(x => x.Subscription.QueryIdentifier)
            .Select(g => new QuerySubscriptionAggregate
            {
                QueryName = g.Key,
                Subscribers = g.Select(x => new QuerySubscriber
                {
                    ConnectionId = x.Connection.ConnectionId,
                    Protocol = x.Connection.Protocol,
                    SubscriptionId = x.Subscription.SubscriptionId,
                    ConnectedAt = x.Subscription.ConnectedAt,
                    LastPingSentAt = x.Subscription.LastPingSentAt,
                    LastPongReceivedAt = x.Subscription.LastPongReceivedAt,
                    LastDataServedAt = x.Subscription.LastDataServedAt,
                    ClientInfo = x.Subscription.ClientInfo
                }).ToList()
            });

    /// <summary>
    /// Observes query health as a real-time stream.
    /// </summary>
    /// <param name="healthTracker">The query health tracker service.</param>
    /// <returns>Observable subject emitting query health snapshots.</returns>
    [AllowAnonymous]
    public static ISubject<QueryHealth> ObserveHealth(IQueryHealthTracker healthTracker)
    {
#pragma warning disable CA2000 // DisposableQueryHealthSubject owns and disposes the BehaviorSubject returned from this method.
        var subject = new BehaviorSubject<QueryHealth>(new QueryHealth
        {
            Connections = healthTracker.GetAllConnectionHealth()
        });
#pragma warning restore CA2000

        var subscription = healthTracker.ObserveHealth().Subscribe(connections =>
            subject.OnNext(new QueryHealth { Connections = connections }));

        // Return a subject that will clean up the subscription when disposed
        return new DisposableQueryHealthSubject(subject, subscription);
    }

    private sealed class DisposableQueryHealthSubject(ISubject<QueryHealth> inner, IDisposable subscription) : ISubject<QueryHealth>, IDisposable
    {
        public void OnCompleted() => inner.OnCompleted();

        public void OnError(Exception error) => inner.OnError(error);

        public void OnNext(QueryHealth value) => inner.OnNext(value);

        public IDisposable Subscribe(IObserver<QueryHealth> observer) => inner.Subscribe(observer);

        public void Dispose()
        {
            subscription.Dispose();
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
