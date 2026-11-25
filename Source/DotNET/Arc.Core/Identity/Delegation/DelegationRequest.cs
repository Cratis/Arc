// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Delegation;

/// <summary>
/// Application-selected delegated capability and destination bound to a verified caller.
/// Contains no request-supplied grant, actor, or authority ceilings.
/// </summary>
/// <param name="Caller">The framework-verified delegated caller, including its credential provenance and grant reference.</param>
/// <param name="Capability">The capability selected by trusted application policy, not request metadata.</param>
/// <param name="Resource">The exact receiving resource, which must match the caller.</param>
/// <param name="Environment">The exact receiving environment, which must match the caller.</param>
public sealed record DelegationRequest(VerifiedCaller Caller, string Capability, string Resource, string Environment);
