// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>Resolves a single distinct value of an application-selected caller claim.</summary>
/// <param name="claimType">The exact claim type to read.</param>
public abstract class CallerClaim(string claimType) : IReadModelKeyProvider
{
    /// <inheritdoc/>
    public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => context.Caller.IsSystem || !context.Caller.IsAuthenticated
        ? ReadModelKeyLookup.Missing
        : context.Caller.Single(claimType);
}
