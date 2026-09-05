// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Collects the shapes an application refers to, keeping one declaration per name.
/// </summary>
/// <remarks>
/// A record an artifact carries is referred to by its simple name and declared once at the top of the document,
/// exactly as a concept is - so the same two questions decide what comes out of here. Which shapes the document
/// declares follows from which ones were reached while a type was being resolved rather than from which ones the
/// application defines, and two records sharing a simple name cannot both be described under it.
/// <para>
/// A name is claimed before what it holds is read, which is what lets a record referring to itself - directly or
/// around a loop - be walked once instead of forever.
/// </para>
/// </remarks>
public class ShapeRegistry
{
    /// <summary>The reason given for a record that carries nothing a declaration could hold.</summary>
    public const string CarriesNothing = "it carries no value a declaration could hold";

    readonly Dictionary<string, TypeModel> _shapes = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> _declaredFrom = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> _undeclarable = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets every shape the document declares, ordered by name.
    /// </summary>
    /// <param name="taken">The names something else already declares, which a shape cannot be declared under.</param>
    /// <returns>The shapes.</returns>
    public IEnumerable<TypeModel> Declared(IReadOnlySet<string> taken) =>
    [
        .. _shapes
            .Where(_ => !taken.Contains(_.Key))
            .Select(_ => _.Value)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets every shape no declaration could be written for, ordered by name.
    /// </summary>
    /// <param name="taken">The names something else already declares, which a shape cannot be declared under.</param>
    /// <returns>The shapes, each with why it was left out.</returns>
    public IEnumerable<UndeclarableShape> Undeclarable(IReadOnlySet<string> taken) =>
    [
        .. _undeclarable
            .Select(_ => new UndeclarableShape(_.Key, _.Value))
            .Concat(_shapes
                .Where(_ => taken.Contains(_.Key))
                .Select(_ => new UndeclarableShape(_declaredFrom[_.Key], $"a concept is already declared as '{_.Key}'")))
            .OrderBy(_ => _.Type, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Registers a record as a shape, reading what it carries through the resolver naming each value.
    /// </summary>
    /// <param name="type">The record to register.</param>
    /// <param name="resolve">The resolver naming the type of one value, which registers whatever it reaches in turn.</param>
    /// <remarks>
    /// The values are read in the order the source declares them, because the order of a record's positional
    /// parameters is what the developer wrote and the document reads best the same way round.
    /// </remarks>
    public void Register(ITypeSymbol type, Func<ITypeSymbol, TypeReferenceModel> resolve)
    {
        var name = type.Name;
        var full = type.ToDisplayString();

        if (_declaredFrom.TryGetValue(name, out var already))
        {
            if (!string.Equals(already, full, StringComparison.Ordinal))
            {
                _undeclarable[full] = $"'{already}' is already declared as '{name}'";
            }

            return;
        }

        _declaredFrom[name] = full;

        var properties = type.DeclaredProperties().Select(_ => new PropertyModel(_.Name, resolve(_.Type))).ToList();
        if (properties.Count == 0)
        {
            _declaredFrom.Remove(name);
            _undeclarable[full] = CarriesNothing;

            return;
        }

        _shapes[name] = new(name, properties);
    }
}
