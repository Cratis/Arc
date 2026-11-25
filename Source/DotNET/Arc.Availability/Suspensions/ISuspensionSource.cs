// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// Defines where the decisions to suspend and resume parts come from, beside the <see cref="IAudienceSource"/>.
/// </summary>
/// <remarks>
/// A product implements this to feed the engine from its own state, for example from the events <c>PartSuspended</c> and
/// <c>PartResumed</c>. Audiences are granted and withdrawn; parts are suspended and resumed.
/// </remarks>
public interface ISuspensionSource
{
    /// <summary>
    /// Gets the decisions that apply to the subject: those at the tenant level for its tenant, at the user level for its user,
    /// and at the system level when the subject is the system.
    /// </summary>
    /// <param name="subject">The <see cref="AudienceSubject"/> to get the decisions for.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
    /// <returns>The suspend and resume decisions in any order; the engine keeps the newest per level and path.</returns>
    ValueTask<IReadOnlyCollection<PartSuspensionDecision>> GetDecisions(AudienceSubject subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to changes, so open sessions can decide again.
    /// </summary>
    /// <param name="handler">The handler called after every suspend or resume.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription.</returns>
    IDisposable OnChanged(Func<SuspensionChange, ValueTask> handler);
}
