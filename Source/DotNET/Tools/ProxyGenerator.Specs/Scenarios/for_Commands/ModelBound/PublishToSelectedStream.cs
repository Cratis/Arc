// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

/// <summary>
/// Publishes a B subscription from inside a different selected principal's command flow.
/// </summary>
[Command]
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Other")]
public record PublishToSelectedStream
{
    /// <summary>Gets the name seen by the command before publishing.</summary>
    public static string? LastPublisher { get; private set; }

    /// <summary>Gets the producing command principal after the emission callback returns.</summary>
    public static string? LastPublisherAfterEmit { get; private set; }

    /// <summary>Publishes to the subject while the selected C principal is ambient.</summary>
    /// <param name="principal">The command's selected principal.</param>
    public void Handle(ICurrentPrincipalAccessor principal)
    {
        LastPublisher = principal.Current?.Identity?.Name;
        PolicyProtectedStream.Emit();
        LastPublisherAfterEmit = principal.Current?.Identity?.Name;
    }
}
