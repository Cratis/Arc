// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a guard that is consulted for every emission an observable query subscription is about to write.
/// </summary>
/// <remarks>
/// Authorization for an observable query is evaluated by the query pipeline when the subscription is established; that
/// verdict gates <em>obtaining</em> the stream. A subscription can then stay open indefinitely, so implement this
/// interface when a verdict must be re-checked while the stream is running — an expired token, an ended session, a
/// revoked role.
/// <para>
/// Guards are opt-in and discovered by convention: implement the interface and it is picked up. With no implementation
/// present, nothing is dispatched and emissions take the same path they always did.
/// </para>
/// <para>
/// Every emission calls this, so keep the implementation fast and prefer cached state over per-emission network calls.
/// A guard that throws fails closed — the emission is dropped and the subscription is terminated.
/// </para>
/// </remarks>
public interface IGuardObservableQueryEmission
{
    /// <summary>
    /// Decides what should happen to an emission that is about to be written to the client.
    /// </summary>
    /// <param name="context">The <see cref="ObservableQueryEmissionContext"/> describing the emission.</param>
    /// <returns>The <see cref="ObservableQueryEmissionVerdict"/> for the emission.</returns>
    Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context);
}
