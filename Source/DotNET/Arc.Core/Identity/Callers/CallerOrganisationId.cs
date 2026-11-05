// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>An organization reference from a confirmed Access tenant binding, never a substitute for membership.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Value">The reference.</param>
internal sealed record CallerOrganisationId(string Value) : ConceptAs<string>(Value);
