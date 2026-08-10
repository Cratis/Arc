// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the system that aggregates all discovered <see cref="IGuardObservableQueryEmission"/> implementations into a
/// single verdict per observable query emission.
/// </summary>
public interface IObservableQueryEmissionGuards
{
    /// <summary>
    /// Gets a value indicating whether any <see cref="IGuardObservableQueryEmission"/> implementation exists.
    /// </summary>
    /// <remarks>
    /// Emission paths check this before doing any work for a guard, so an application without guards pays nothing per
    /// emission — no context is built and no dispatch happens.
    /// </remarks>
    bool HasGuards { get; }

    /// <summary>
    /// Asks every discovered guard about an emission and aggregates their verdicts.
    /// </summary>
    /// <param name="context">The <see cref="ObservableQueryEmissionContext"/> describing the emission.</param>
    /// <returns>The most restrictive <see cref="ObservableQueryEmissionVerdict"/> any guard returned.</returns>
    /// <remarks>
    /// The aggregate is most-restrictive-wins, and the first
    /// <see cref="ObservableQueryEmissionVerdict.DenyAndTerminate"/> short-circuits the remaining guards. A guard that
    /// throws is treated as <see cref="ObservableQueryEmissionVerdict.DenyAndTerminate"/> — the failure is logged and
    /// never rethrown, because a guard that fails open would leave an application believing it is protected.
    /// </remarks>
    Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context);
}
