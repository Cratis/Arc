// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Result of an authorization evaluation.
/// </summary>
/// <param name="IsAuthorized">Indicates whether the authorization was successful.</param>
/// <param name="FailureReason">Optional reason for authorization failure.</param>
public record AuthorizationResult(bool IsAuthorized, string? FailureReason = null)
{
    /// <summary>
    /// Creates a successful authorization result.
    /// </summary>
    public static AuthorizationResult Success => new(true);

    /// <summary>
    /// Creates a failed authorization result.
    /// </summary>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>A failed <see cref="AuthorizationResult"/>.</returns>
    public static AuthorizationResult Failure(string reason) => new(false, reason);
}