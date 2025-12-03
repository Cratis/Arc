// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the strongly-typed identity details returned by <see cref="IProvideIdentityDetails{TDetails}"/>.
/// </summary>
/// <typeparam name="TDetails">The type of details.</typeparam>
/// <param name="IsUserAuthorized">Whether or not the user is authorized.</param>
/// <param name="Details">The actual details.</param>
public record IdentityDetails<TDetails>(bool IsUserAuthorized, TDetails Details)
    where TDetails : class;
