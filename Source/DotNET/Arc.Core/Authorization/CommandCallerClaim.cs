// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>An immutable selected-caller claim, excluding the actor chain.</summary>
/// <param name="Type">The claim type.</param>
/// <param name="Value">The claim value.</param>
/// <param name="Issuer">The claim issuer.</param>
public sealed record CommandCallerClaim(string Type, string Value, string Issuer);
