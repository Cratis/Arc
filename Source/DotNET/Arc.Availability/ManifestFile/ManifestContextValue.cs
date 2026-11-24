// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A context value a gate can read, as declared.
/// </summary>
/// <param name="Name">The name under <c>$context</c>.</param>
/// <param name="Scope">The level it is declared at, from the module down, joined with <c>/</c>; empty for the application.</param>
/// <param name="Required">Whether the value must exist.</param>
/// <param name="Origin">Where it comes from, as authored; empty when unknown.</param>
public record ManifestContextValue(string Name, string Scope, bool Required, string Origin = "");
