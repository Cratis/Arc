// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

/// <summary>Captures the actor seen by a nested no-scheme pipeline execution.</summary>
[Command]
[Authorize]
public record NestedCoreChild
{
    /// <summary>Gets the actual child handler principal.</summary>
    public static string? HandlerActor { get; private set; }

    /// <summary>Captures the current child actor.</summary>
    /// <param name="principal">The Arc principal accessor.</param>
    public void Handle(ICurrentPrincipalAccessor principal) => HandlerActor = principal.Current?.Identity?.Name;
}
