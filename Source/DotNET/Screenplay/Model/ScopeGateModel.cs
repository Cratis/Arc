// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A gate that a scope marker states for the module, feature or slice of its namespace.
/// </summary>
/// <param name="Scope">The segments of the namespace of the marker.</param>
/// <param name="Gate">The gate.</param>
/// <param name="Location">Where the marker is declared, for use in diagnostics.</param>
public record ScopeGateModel(IReadOnlyList<string> Scope, AvailabilityGateModel Gate, string Location);
