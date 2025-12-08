// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Defines the system that provides identity details.
/// </summary>
public interface IProvideIdentityDetails
{
    /// <summary>
    /// Provide the details.
    /// </summary>
    /// <param name="context">The <see cref="IdentityProviderContext"/>.</param>
    /// <returns>The details.</returns>
    /// <remarks>
    /// The result of this will end up being serialized as JSON.
    /// </remarks>
    Task<IdentityDetails> Provide(IdentityProviderContext context);
}

/// <summary>
/// Represents a wrapper interface for providers of strongly-typed identity details to capture type details of the details provided.
/// </summary>
/// <typeparam name="TDetails">The type of details to provide.</typeparam>
public interface IProvideIdentityDetails<TDetails> : IProvideIdentityDetails
    where TDetails : class;