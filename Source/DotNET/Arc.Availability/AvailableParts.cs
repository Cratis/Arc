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
/// <param name="Unavailable">The commands and read models that are not available, with the slice each belongs to.</param>
[ReadModel]
[Path("/.cratis/availability")]
public sealed record AvailableParts(IEnumerable<UnavailablePart> Unavailable)
{
    /// <summary>
    /// Observes the parts that are not available to the current context.
    /// </summary>
    /// <param name="parts">The <see cref="IUnavailableParts"/> for the current request.</param>
    /// <param name="part">The full name of one part to ask about; every part is decided when it is not given.</param>
    /// <param name="arguments">The arguments of that part as a JSON object, for a gate that reads <c>arguments.name</c>.</param>
    /// <returns>A subject that emits the parts that are not available, each time the decision changes.</returns>
    /// <remarks>
    /// With a <paramref name="part"/> the answer lists that part when it is not available for those arguments, and is empty
    /// when it is available. The same gate decides as for an execution, so there is no second rule to keep in step.
    /// </remarks>
    public static ISubject<AvailableParts> ObserveUnavailable(IUnavailableParts parts, string? part = null, string? arguments = null) =>
        new AvailablePartsSubject(parts, string.IsNullOrEmpty(part) ? null : PartQuestion.From(part, arguments));
}
