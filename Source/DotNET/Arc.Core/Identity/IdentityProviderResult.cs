// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents the result of providing identity.
/// </summary>
/// <param name="Id">Unique identifier for the identity.</param>
/// <param name="Name">Name of the identity.</param>
/// <param name="IsAuthenticated">Indicates whether the user is authenticated.</param>
/// <param name="IsAuthorized">Indicates whether the user is authorized.</param>
/// <param name="Details">The resolved details.</param>
public record IdentityProviderResult(IdentityId Id, IdentityName Name, bool IsAuthenticated, bool IsAuthorized, object Details)
{
    /// <summary>
    /// Represents an anonymous identity result.
    /// </summary>
    public static readonly IdentityProviderResult Anonymous = new(IdentityId.Empty, IdentityName.Empty, false, false, new { });

    /// <summary>
    /// Represents an unauthorized identity result.
    /// </summary>
    public static readonly IdentityProviderResult Unauthorized = new(IdentityId.Empty, IdentityName.Empty, true, false, new { });
}

/// <summary>
/// Represents the result of providing identity with strongly typed details.
/// </summary>
/// <typeparam name="TDetails">Type of the details.</typeparam>
/// <param name="Id">Unique identifier for the identity.</param>
/// <param name="Name">Name of the identity.</param>
/// <param name="IsAuthenticated">Indicates whether the user is authenticated.</param>
/// <param name="IsAuthorized">Indicates whether the user is authorized.</param>
/// <param name="Details">The resolved details.</param>
public record IdentityProviderResult<TDetails>(IdentityId Id, IdentityName Name, bool IsAuthenticated, bool IsAuthorized, TDetails Details);

#pragma warning restore SA1402 // File may only contain a single type
