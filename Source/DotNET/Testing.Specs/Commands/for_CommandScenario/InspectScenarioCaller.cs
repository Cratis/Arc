// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.for_CommandScenario;

[Command]
public record InspectScenarioCaller
{
    public Observation Handle(ICurrentPrincipalAccessor accessor)
    {
        AuthorizationCommandIdentity.TryGetCaller(accessor.Current, out var caller, out _);
        return new(accessor.Current, new CallerSubject().Resolve(new(caller!, typeof(object))));
    }

    public record Observation(ClaimsPrincipal? Principal, ReadModelKeyLookup Subject);
}
