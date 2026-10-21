// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Makes validation refusals available while symbol-keyed generation dependencies can still be recovered.
/// </summary>
/// <param name="models">The application semantic models.</param>
/// <param name="types">The concept registry.</param>
/// <param name="validators">Every validator declared across the application.</param>
/// <param name="cap">The requested version boundary.</param>
internal class GeneratedValidationAdmission(SemanticModels models, TypeRegistry types, IReadOnlyList<INamedTypeSymbol> validators, SemanticVersion? cap)
{
    readonly Dictionary<string, string?> _failures = new(StringComparer.Ordinal);

    /// <summary>
    /// Checks the same complete generation contract used by emission, before handler recovery loses dependency evidence.
    /// </summary>
    /// <param name="type">The generated concept type.</param>
    /// <returns>The refusal, or null when the rules permit generation.</returns>
    internal string? Failure(ITypeSymbol type)
    {
        var key = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();
        if (_failures.TryGetValue(key, out var known))
        {
            return known;
        }
        var declared = validators.Where(validator => ValidationReader.ValidatedTypeOf(validator)?.ToDisplayString() == key).ToList();
        if (declared.Count == 0)
        {
            return null;
        }
        var name = types.Resolve(type).Name;
        var concept = types.Concepts.SingleOrDefault(concept => concept.Name == name);
        var reader = new ValidationReader(models, new ScreenplayDiagnostics());
        var recovered = concept is null ? null : concept with
        {
            Validations = declared.SelectMany(validator => reader.Read(validator, key)).ToList(),
            ValidationRecoveryComplete = declared.TrueForAll(reader.IsComplete)
        };
        var failure = recovered is null ? "the generated concept declaration could not be recovered" : GenerationAdmission.Failure(recovered, cap);
        _failures[key] = failure;

        return failure;
    }
}
