// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents what is not available to the current context, as an observable query for clients.
/// </summary>
/// <remarks>
/// Clients hide what is listed and show everything else. The query emits the current decision first and again each time
/// it changes, so open sessions update without a reload. A decision that could not be made is a retryable error, never a
/// list: clients keep what they last knew.
/// </remarks>
/// <param name="Unavailable">The names of the commands and read models that are not available, as <see cref="PartName"/> gives them.</param>
[ReadModel]
[Path("/.cratis/availability")]
public sealed record AvailableParts(IEnumerable<string> Unavailable)
{
    /// <summary>
    /// Observes the parts that are not available to the current context.
    /// </summary>
    /// <param name="parts">The <see cref="IUnavailableParts"/> for the current request.</param>
    /// <returns>A subject that emits the parts that are not available, each time the decision changes.</returns>
    public static ISubject<AvailableParts> ObserveUnavailable(IUnavailableParts parts) =>
        new AvailablePartsSubject(parts);
}
