// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Path = Cratis.Arc.Queries.ModelBound.PathAttribute;

namespace VerifiedCallerAcceptance;

/// <summary>
/// Demonstrates product authorization and credential scope narrowing before dispatch.
/// </summary>
[Command, Path("/articles/publish"), EndpointGroup("capabilities"), Authorize, Roles("editor"), RequiresScope("content:write")]
public record PublishArticle
{
    /// <summary>
    /// Returns the subject Arc exposes to causation after authorization.
    /// </summary>
    /// <param name="principals">The effective Arc principal accessor.</param>
    /// <param name="counts">Dispatch counter.</param>
    /// <returns>The verified person subject.</returns>
    public string Handle(ICurrentPrincipalAccessor principals, InvocationCounts counts)
    {
        counts.Published();
        return principals.Current!.FindFirst("sub")!.Value;
    }
}
