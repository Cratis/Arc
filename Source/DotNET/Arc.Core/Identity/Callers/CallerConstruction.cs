// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>The builder's caller or refusal, never a partially constructed caller.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Caller">The constructed caller, or null on refusal.</param>
/// <param name="Refusal">The refusal, or null on success.</param>
internal sealed record CallerConstruction(VerifiedCaller? Caller, CallerRefusal? Refusal);
