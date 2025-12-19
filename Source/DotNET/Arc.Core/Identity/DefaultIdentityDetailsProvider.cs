// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a default <see cref="IProvideIdentityDetails"/> implementation.
/// </summary>
public class DefaultIdentityDetailsProvider : IProvideIdentityDetails
{
    /// <inheritdoc/>
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        return Task.FromResult(new IdentityDetails(true, new { }));
    }
}
