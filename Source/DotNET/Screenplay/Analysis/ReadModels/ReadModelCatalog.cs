// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.ReadModels;

/// <summary>
/// Collects the read models of the whole application, keeping one per type.
/// </summary>
/// <remarks>
/// A read model is reached from more than one place - the <c>[ReadModel]</c> type itself, the projection building it
/// and the reducer folding into it - and from more than one project, so the same type arrives several times and is
/// kept once. Two different types sharing a simple name are a different matter: the document refers to a read model
/// by its simple name only, so neither can be declared without the other's references resolving to it.
/// <para>
/// What a read model holds is not read here. Reading a property commits the document to declaring the concepts and
/// shapes it reaches, and that is only true of a read model the document goes on to declare - which is decided once
/// every slice is known.
/// </para>
/// </remarks>
public class ReadModelCatalog
{
    readonly Dictionary<string, ReadModelCandidate> _byFullName = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets every read model whose simple name no other read model shares, ordered by name.
    /// </summary>
    public IEnumerable<ReadModelCandidate> Unambiguous =>
    [
        .. _byFullName.Values
            .GroupBy(_ => _.Shape.Name, StringComparer.Ordinal)
            .Where(_ => _.Count() == 1)
            .Select(_ => _.Single())
            .OrderBy(_ => _.Shape.Name, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets the full names of every read model sharing its simple name with another one, ordered.
    /// </summary>
    public IEnumerable<string> Ambiguous =>
    [
        .. _byFullName
            .GroupBy(_ => _.Value.Shape.Name, StringComparer.Ordinal)
            .Where(_ => _.Count() > 1)
            .SelectMany(_ => _.Select(entry => entry.Key))
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets whether a read model has already been declared.
    /// </summary>
    /// <param name="fullName">The full name of the read model type.</param>
    /// <returns>True when it has.</returns>
    public bool Contains(string fullName) => _byFullName.ContainsKey(fullName);

    /// <summary>
    /// Declares a read model, keeping the first declaration of a type.
    /// </summary>
    /// <param name="fullName">The full name of the read model type.</param>
    /// <param name="candidate">The read model.</param>
    public void Declare(string fullName, ReadModelCandidate candidate) => _byFullName.TryAdd(fullName, candidate);
}
