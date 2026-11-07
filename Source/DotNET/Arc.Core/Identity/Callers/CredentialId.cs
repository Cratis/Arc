// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A verifier-supplied credential identifier, not a raw secret.
/// </summary>
/// <param name="Value">The identifier.</param>
public sealed record CredentialId(string Value) : ConceptAs<string>(Value);
