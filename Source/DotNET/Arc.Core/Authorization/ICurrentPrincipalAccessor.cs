// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defines a system for accessing the principal that is currently executing, independent of transport.
/// </summary>
public interface ICurrentPrincipalAccessor
{
    /// <summary>
    /// Gets the <see cref="ClaimsPrincipal"/> that is currently executing — the HTTP request principal while a
    /// request is in progress, otherwise the principal established by a server-side execution scope, or
    /// <see langword="null"/> when neither is present.
    /// </summary>
    ClaimsPrincipal? Current { get; }
}
