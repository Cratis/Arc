// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defines a system for accessing the current server-side execution principal, if any.
/// </summary>
public interface ISystemExecutionAccessor
{
    /// <summary>
    /// Gets the <see cref="ClaimsPrincipal"/> established by the current <see cref="ISystemExecution"/> scope,
    /// or <see langword="null"/> when no server-side execution scope is active.
    /// </summary>
    ClaimsPrincipal? Current { get; }
}
