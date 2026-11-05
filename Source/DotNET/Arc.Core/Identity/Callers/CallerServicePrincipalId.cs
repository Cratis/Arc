// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>A verified service principal identifier.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Value">The identifier.</param>
internal sealed record CallerServicePrincipalId(string Value) : ConceptAs<string>(Value);
