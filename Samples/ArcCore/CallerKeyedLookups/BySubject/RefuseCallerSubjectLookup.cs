// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Samples.Features.CallerKeyedLookups.BySubject;

/// <summary>
/// Demonstrates discovery and refusal of CallerSubject without a Chronicle binder.
/// </summary>
[Command, AllowAnonymous]
public record RefuseCallerSubjectLookup
{
    /// <summary>
    /// Must not run: the lookup declaration prevents ordinary service-provider fallback.
    /// </summary>
    /// <param name="services">An otherwise injectable dependency with an unsupported lookup declaration.</param>
    /// <returns>The service-provider type if the declaration is incorrectly ignored.</returns>
    public string Handle([LookUpBy<CallerSubject>] IServiceProvider services) => services.GetType().Name;
}
