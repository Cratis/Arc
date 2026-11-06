// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace VerifiedCallerAcceptance;

/// <summary>
/// Demonstrates a separate scope requirement on a capability query.
/// </summary>
/// <param name="Subject">The effective reader subject.</param>
[ReadModel, EndpointGroup("capabilities"), Authorize]
public record ReadArticles(string Subject)
{
    /// <summary>
    /// Reads the effective subject using the public principal accessor.
    /// </summary>
    /// <param name="principals">The effective Arc principal accessor.</param>
    /// <returns>The current reader.</returns>
    [Path("/articles"), RequiresScope("read")]
    public static ReadArticles All(ICurrentPrincipalAccessor principals) => new(principals.Current!.FindFirst("sub")!.Value);
}
