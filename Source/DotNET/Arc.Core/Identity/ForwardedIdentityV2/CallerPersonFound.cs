// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>An explicit persisted-person lookup result.</summary>
internal sealed class CallerPersonFound
{
    CallerPersonFound(CallerPersonId? person) => Person = person;

    /// <summary>Gets the persisted identifier, if found.</summary>
    internal CallerPersonId? Person { get; }

    /// <summary>Creates a successful lookup result.</summary>
    /// <param name="person">The persisted identifier.</param>
    /// <returns>The found result.</returns>
    internal static CallerPersonFound Found(CallerPersonId person) => new(person);

    /// <summary>Creates an unsuccessful lookup result.</summary>
    /// <returns>The not-found result.</returns>
    internal static CallerPersonFound NotFound() => new(null);
}
