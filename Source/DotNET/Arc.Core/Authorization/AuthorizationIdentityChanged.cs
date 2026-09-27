// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// The exception that is thrown when an asynchronous authorization verdict no longer certifies the execution caller.
/// </summary>
public sealed class AuthorizationIdentityChanged : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationIdentityChanged"/> class.
    /// </summary>
    public AuthorizationIdentityChanged() : base("The authorization identity changed before invocation.")
    {
    }
}
