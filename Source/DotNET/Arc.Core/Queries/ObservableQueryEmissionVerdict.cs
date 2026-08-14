// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the verdict an <see cref="IGuardObservableQueryEmission"/> gives for a single observable query emission.
/// </summary>
/// <remarks>
/// The values are ordered from least to most restrictive, so aggregating several guards is a matter of keeping the
/// highest verdict any of them returned.
/// </remarks>
public enum ObservableQueryEmissionVerdict
{
    /// <summary>
    /// The emission is written to the client unchanged.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// The emission is withheld, and the subscription stays live so a later emission can be delivered again.
    /// </summary>
    Suppress = 1,

    /// <summary>
    /// The emission is withheld, the client is told it is no longer authorized, and the subscription is torn down.
    /// </summary>
    DenyAndTerminate = 2
}
