// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Whether client metadata was declared or authenticated.
/// </summary>
public enum CallerClientAuthentication
{
    /// <summary>Metadata only; grants no actor authority.</summary>
    Declared = 0,

    /// <summary>The credential verifier authenticated the client.</summary>
    Authenticated = 1
}
