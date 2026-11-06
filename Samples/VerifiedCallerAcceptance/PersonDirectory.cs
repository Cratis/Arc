// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace VerifiedCallerAcceptance;

/// <summary>
/// Maps the sample's two persisted realm accounts without provisioning new people.
/// </summary>
public sealed class PersonDirectory : ICallerPersonDirectory
{
    /// <inheritdoc/>
    public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken)
    {
        var known = accountKey.Issuer == VerifiedCallerAcceptanceSettings.RealmIssuer &&
            (accountKey.Subject == VerifiedCallerAcceptanceSettings.Editor || accountKey.Subject == VerifiedCallerAcceptanceSettings.Reader);
        return Task.FromResult(known ? CallerPersonFound.Found(new(accountKey.Subject)) : CallerPersonFound.NotFound());
    }
}
