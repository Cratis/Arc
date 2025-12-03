// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Defines the system that provides strongly-typed identity details.
/// </summary>
/// <typeparam name="TDetails">The type of details to provide.</typeparam>
public interface IProvideIdentityDetails<TDetails> : IProvideIdentityDetails
    where TDetails : class
{
    /// <summary>
    /// Provide the strongly-typed details.
    /// </summary>
    /// <param name="context">The <see cref="IdentityProviderContext"/>.</param>
    /// <returns>The details.</returns>
    /// <remarks>
    /// The result of this will end up being serialized as JSON.
    /// </remarks>
    Task<IdentityDetails<TDetails>> ProvideDetails(IdentityProviderContext context);

    /// <inheritdoc/>
#pragma warning disable CA1033 // Interface methods should be callable by child types - By design, this provides a default implementation wrapping the typed call
    async Task<IdentityDetails> IProvideIdentityDetails.Provide(IdentityProviderContext context)
    {
        var result = await ProvideDetails(context);
        return new IdentityDetails(result.IsUserAuthorized, result.Details);
    }
#pragma warning restore CA1033
}
