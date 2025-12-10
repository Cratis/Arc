// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication;

/// <summary>
/// Represents an authentication failure.
/// </summary>
/// <param name="Reason">The reason for the authentication failure.</param>
public record AuthenticationFailure(AuthenticationFailureReason Reason);
