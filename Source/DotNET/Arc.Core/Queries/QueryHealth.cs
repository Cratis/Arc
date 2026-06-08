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
    /// Observes query health as a real-time stream.
    /// </summary>
    /// <param name="healthTracker">The query health tracker service.</param>
    /// <returns>Observable subject emitting query health snapshots.</returns>
    [AllowAnonymous]
    public static ISubject<QueryHealth> ObserveHealth(IQueryHealthTracker healthTracker)
    {
        var subject = new BehaviorSubject<QueryHealth>(new QueryHealth
        {
            Connections = healthTracker.GetAllConnectionHealth()
        });

        var subscription = healthTracker.ObserveHealth().Subscribe(connections =>
        {
            subject.OnNext(new QueryHealth { Connections = connections });
        });

        // Return a subject that will clean up the subscription when disposed
        return subject;
    }
}
