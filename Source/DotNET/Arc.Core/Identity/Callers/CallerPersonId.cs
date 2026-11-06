// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>A verified person identifier.</summary>
/// <param name="Value">The identifier.</param>
public sealed record CallerPersonId(string Value) : ConceptAs<string>(Value);
