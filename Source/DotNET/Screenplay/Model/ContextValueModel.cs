// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a request-scoped value the code provides from a read model query.
/// </summary>
/// <param name="Name">The name the value is read by.</param>
/// <param name="Query">The name of the snapshot query that resolves it.</param>
/// <param name="Key">The <c>$context</c> path the query is keyed by, such as <c>arguments.vesselId</c>.</param>
/// <param name="Scope">The module, feature and slice names the value is declared at. Empty for the application.</param>
/// <param name="Required">Whether a request is refused when the value is absent.</param>
/// <param name="Location">Where the value is declared, for use in diagnostics.</param>
public record ContextValueModel(string Name, string Query, string Key, IReadOnlyList<string> Scope, bool Required, string Location);
