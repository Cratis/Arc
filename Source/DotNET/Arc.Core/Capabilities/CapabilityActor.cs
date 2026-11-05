// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Identifies the current verified delegated actor, not a declared client.
/// </summary>
/// <param name="Issuer">The actor issuer.</param>
/// <param name="Subject">The actor subject.</param>
public sealed record CapabilityActor(string Issuer, string Subject);
