// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed class PersonDirectory(bool found = true) : ICallerPersonDirectory
{
    internal int Calls { get; private set; }
    public Task<CallerPersonFound> Find(Callers.CallerAccountKey accountKey, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(found ? CallerPersonFound.Found(new("persisted-person")) : CallerPersonFound.NotFound());
    }
}
