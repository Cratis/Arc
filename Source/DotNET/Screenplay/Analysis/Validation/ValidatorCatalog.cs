// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Holds the rules every validator in the compilation declares, keyed by what it validates.
/// </summary>
/// <remarks>
/// A validator does not have to live next to what it validates, so they are collected once for the whole
/// compilation and looked up by type rather than discovered per slice.
/// </remarks>
public class ValidatorCatalog
{
    readonly Dictionary<string, List<ValidationRuleModel>> _rules = new(StringComparer.Ordinal);
    readonly HashSet<string> _incomplete = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<ValidationRuleModel>> _sources = new(StringComparer.Ordinal);

    /// <summary>
    /// Collects every validator a catalogue of types declares.
    /// </summary>
    /// <param name="catalog">The catalogue to read.</param>
    /// <param name="reader">The <see cref="ValidationReader"/> reading each validator.</param>
    /// <returns>The <see cref="ValidatorCatalog"/>.</returns>
    public static ValidatorCatalog From(ArtifactCatalog catalog, ValidationReader reader)
    {
        var validators = new ValidatorCatalog();

        foreach (var type in catalog.Types.Where(_ => _ is { IsAbstract: false, TypeKind: TypeKind.Class }))
        {
            if (ValidationReader.ValidatedTypeOf(type) is not { } validated)
            {
                continue;
            }

            validators.Add(validated, reader.Read(type, type.Namespace()));
            if (reader.SourceReference(type) is { } reference)
            {
                var key = KeyFor(validated);
                if (!validators._sources.TryGetValue(key, out var sources))
                {
                    validators._sources[key] = sources = [];
                }
                sources.Add(reference);
            }
            if (!reader.IsComplete(type))
            {
                validators._incomplete.Add(KeyFor(validated));
            }
        }

        return validators;
    }

    /// <summary>
    /// Gets the rules declared for a type.
    /// </summary>
    /// <param name="type">The type to look up.</param>
    /// <returns>The rules, empty when nothing validates the type.</returns>
    public IEnumerable<ValidationRuleModel> For(ITypeSymbol type) =>
        _rules.TryGetValue(KeyFor(type), out var rules) ? rules : [];

    /// <summary>
    /// Checks whether all default validation rules for a type were recovered.
    /// </summary>
    /// <param name="type">The validated type.</param>
    /// <returns>Whether its validation contract is complete.</returns>
    internal bool IsComplete(ITypeSymbol type) => !_incomplete.Contains(KeyFor(type));

    /// <summary>
    /// Gets opaque references preserving the original validator implementations.
    /// </summary>
    /// <param name="type">The validated type.</param>
    /// <returns>The source attachments.</returns>
    internal IEnumerable<ValidationRuleModel> SourcesFor(ITypeSymbol type) => _sources.GetValueOrDefault(KeyFor(type)) ?? [];

    /// <summary>
    /// Gets the key a type is looked up by.
    /// </summary>
    /// <param name="type">The type to key.</param>
    /// <returns>The key.</returns>
    static string KeyFor(ITypeSymbol type) => type.ToDisplayString();

    /// <summary>
    /// Adds the rules a validator declares.
    /// </summary>
    /// <param name="validated">The type being validated.</param>
    /// <param name="rules">The rules to add.</param>
    void Add(ITypeSymbol validated, IEnumerable<ValidationRuleModel> rules)
    {
        var key = KeyFor(validated);
        if (!_rules.TryGetValue(key, out var declared))
        {
            declared = [];
            _rules[key] = declared;
        }

        declared.AddRange(rules);
    }
}
