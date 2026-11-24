// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

[Command]
public record NestedEvaluationCommand(IServiceProvider Provider, ClaimsPrincipal Principal)
{
    public Task<CommandAuthorizationResult> Handle() => CommandAuthorization.For<RoleCommand>(Provider, Principal);
}
