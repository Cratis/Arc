// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>A verifier's checked proof facts; not a proof parser or replay cache.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Outcome">The verification result.</param>
/// <param name="KeyThumbprint">The verified proof key thumbprint.</param>
/// <param name="Jti">The replay-checked proof identifier.</param>
internal sealed record CallerProof(CallerProofOutcome Outcome, string? KeyThumbprint = null, string? Jti = null);
