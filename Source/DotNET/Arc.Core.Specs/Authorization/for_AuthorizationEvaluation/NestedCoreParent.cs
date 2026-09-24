// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

/// <summary>Exercises a no-scheme nested command under a different trusted server-side actor.</summary>
[Command]
[Authorize]
public record NestedCoreParent
{
    /// <summary>Gets whether the nested pipeline accepted the second actor.</summary>
    public static bool ChildAuthorized { get; private set; }

    /// <summary>Gets the outer actor after the nested scope was restored.</summary>
    public static string? RestoredActor { get; private set; }

    /// <summary>Executes a child command in the same pipeline provider under another no-scheme actor.</summary>
    /// <param name="system">The trusted actor scope.</param>
    /// <param name="pipeline">The actual command pipeline.</param>
    /// <param name="services">The outer execution provider.</param>
    /// <param name="principal">The current Arc principal.</param>
    /// <returns>The nested execution.</returns>
    public async Task Handle(ISystemExecution system, ICommandPipeline pipeline, IServiceProvider services, ICurrentPrincipalAccessor principal)
    {
        using (system.As(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "child")], "test"))))
        {
            ChildAuthorized = (await pipeline.Execute(new NestedCoreChild(), services)).IsAuthorized;
        }

        RestoredActor = principal.Current?.Identity?.Name;
    }
}
