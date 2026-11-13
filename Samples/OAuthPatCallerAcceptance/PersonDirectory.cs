// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace OAuthPatCallerAcceptance;

/// <summary>Maps a persisted realm account; never provisions a person from a token.</summary>
/// <param name="store">The sample's persisted directory state.</param>
public sealed class PersonDirectory(SampleIdentityStore store) : ICallerPersonDirectory
{
    /// <inheritdoc/>
    public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken) =>
        Task.FromResult(store.PersonExists && accountKey == new CallerAccountKey(SampleSettings.Issuer, SampleSettings.Subject) ? CallerPersonFound.Found(new("persisted-person")) : CallerPersonFound.NotFound());
}
