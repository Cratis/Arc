// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>The verifier's sender-proof result, including replay protection.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal enum CallerProofOutcome
{
    /// <summary>No proof was supplied.</summary>
    NotPresented = 0,

    /// <summary>The proof and replay check succeeded.</summary>
    Verified = 1,

    /// <summary>The proof key did not match.</summary>
    WrongKey = 2,

    /// <summary>The proof identifier was already used.</summary>
    Replayed = 3
}
