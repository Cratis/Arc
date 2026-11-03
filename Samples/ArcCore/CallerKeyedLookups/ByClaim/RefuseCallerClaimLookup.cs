// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Samples.Features.CallerKeyedLookups.ByClaim;

/// <summary>
/// Demonstrates that ArcCore refuses caller-keyed parameters without a Chronicle binder.
/// </summary>
[Command, AllowAnonymous]
public record RefuseCallerClaimLookup
{
    /// <summary>
    /// Must not run: the lookup declaration prevents ordinary service-provider fallback.
    /// </summary>
    /// <param name="services">An otherwise injectable dependency with an unsupported lookup declaration.</param>
    /// <returns>The service-provider type if the declaration is incorrectly ignored.</returns>
    public string Handle([LookUpBy<LoginClaim>] IServiceProvider services) => services.GetType().Name;
}
