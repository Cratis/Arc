// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Retains proven case-table provenance alongside concrete models without changing their equality.
/// </summary>
/// <param name="symbolic">The scenario read once with placeholders.</param>
/// <param name="parameters">The single target types inferred for those placeholders.</param>
/// <param name="rows">The independently read and compared literal rows.</param>
internal sealed class SpecificationCaseTable(
    SpecificationModel symbolic,
    IReadOnlyList<(string Name, TypeReferenceModel Type)> parameters,
    IReadOnlyList<(string Name, SpecificationModel Model, IReadOnlyList<PropertyMappingModel> Values)> rows)
{
    static readonly ConditionalWeakTable<SpecificationModel, SpecificationCaseTable> _tables = new();

    /// <summary>
    /// Gets the shared symbolic scenario.
    /// </summary>
    internal SpecificationModel Symbolic { get; } = symbolic;

    /// <summary>
    /// Gets the parameter declarations in constructor order.
    /// </summary>
    internal IReadOnlyList<(string Name, TypeReferenceModel Type)> Parameters { get; } = parameters;

    /// <summary>
    /// Gets every row in deterministic class-name order.
    /// </summary>
    internal IReadOnlyList<(string Name, SpecificationModel Model, IReadOnlyList<PropertyMappingModel> Values)> Rows { get; } = rows;

    /// <summary>
    /// Gets a concrete model's proven table.
    /// </summary>
    /// <param name="model">The original concrete row model.</param>
    /// <returns>The table, or null for an ordinary or nonuniform scenario.</returns>
    internal static SpecificationCaseTable? For(SpecificationModel model) => _tables.TryGetValue(model, out var table) ? table : null;

    /// <summary>
    /// Registers this proven table for every concrete row and its symbolic scenario.
    /// </summary>
    internal void Register()
    {
        _tables.Add(Symbolic, this);
        foreach (var row in Rows)
        {
            _tables.Add(row.Model, this);
        }
    }
}
