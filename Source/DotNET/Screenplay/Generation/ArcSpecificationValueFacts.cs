// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

using static Cratis.Arc.Screenplay.Generation.ArcSpecificationFacts;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Converts exact authored Arc specification literals into neutral value facts.
/// </summary>
internal static class ArcSpecificationValueFacts
{
    /// <summary>
    /// Adds one value fact when its literal, type, identity, and source evidence are exact.
    /// </summary>
    /// <param name="context">The analyzed application projects.</param>
    /// <param name="evidenceFor">The exact evidence factory.</param>
    /// <param name="valueEvidence">Value-expression evidence by legacy model reference.</param>
    /// <param name="step">The owning neutral step.</param>
    /// <param name="artifact">The exact source artifact.</param>
    /// <param name="value">The recovered legacy value.</param>
    /// <param name="values">The atomic value fact buffer.</param>
    /// <param name="key">The value key when successful.</param>
    /// <param name="reason">The blocking reason when unsuccessful.</param>
    /// <returns><see langword="true"/> when the value fact was added.</returns>
    public static bool TryAdd(
        DotNetAnalysisContext context,
        Func<Location, string?, Evidence> evidenceFor,
        IReadOnlyDictionary<PropertyMappingModel, Location> valueEvidence,
        SpecificationStepKey step,
        INamedTypeSymbol artifact,
        PropertyMappingModel value,
        List<SpecificationValueFact> values,
        out SpecificationValueKey? key,
        out string? reason)
    {
        key = null;
        reason = null;
        if (value.Source is not LiteralSource literal || !valueEvidence.TryGetValue(value, out var source))
        {
            reason = $"value '{value.Property}' is not an exact authored literal";
            return false;
        }

        var property = artifact.DeclaredProperties().SingleOrDefault(_ => string.Equals(_.Name, value.Property, StringComparison.Ordinal));
        if (property is null || !TryValue(literal.Value, out var kind, out var scalar))
        {
            reason = $"value '{artifact.Name}.{value.Property}' has an unsupported type or shape";
            return false;
        }

        key = new() { Step = step, Path = [value.Property] };
        values.Add(new()
        {
            Id = FactId("value", step.Scenario.Scenario, $"{step.Index.ToString(CultureInfo.InvariantCulture)}:{value.Property}"),
            Subject = new SubjectId { Value = $"{step.Scenario.Scenario.Value}/step/{step.Index.ToString(CultureInfo.InvariantCulture)}/value/{value.Property}" },
            Evidence = evidenceFor(source, "The authored construction states this exact literal value"),
            Definition = new SpecificationValueDefinition
            {
                Key = key,
                Kind = kind,
                Type = TypeReference(property.Type, context),
                Scalar = scalar
            }
        });
        return true;
    }
}
