// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Types;
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
        var property = artifact.DeclaredProperties().SingleOrDefault(_ => string.Equals(_.Name, value.Property, StringComparison.Ordinal));
        if (property is null)
        {
            reason = $"value '{artifact.Name}.{value.Property}' has no exact declared property";
            return false;
        }

        return TryAddAt(
            context,
            evidenceFor,
            valueEvidence,
            step,
            [value.Property],
            property.Type,
            value,
            values,
            out key,
            out reason);
    }

    /// <summary>
    /// Adds one exact value at its semantic path using the legacy Arc specification type conversion.
    /// </summary>
    /// <param name="context">The analyzed application projects.</param>
    /// <param name="evidenceFor">The exact evidence factory.</param>
    /// <param name="valueEvidence">Value-expression evidence by legacy model reference.</param>
    /// <param name="step">The owning neutral step.</param>
    /// <param name="path">The exact semantic value path.</param>
    /// <param name="type">The exact formal type.</param>
    /// <param name="value">The recovered exact value.</param>
    /// <param name="values">The atomic value fact buffer.</param>
    /// <param name="key">The value key when successful.</param>
    /// <param name="reason">The blocking reason when unsuccessful.</param>
    /// <returns><see langword="true"/> when the value fact was added.</returns>
    public static bool TryAddAt(
        DotNetAnalysisContext context,
        Func<Location, string?, Evidence> evidenceFor,
        IReadOnlyDictionary<PropertyMappingModel, Location> valueEvidence,
        SpecificationStepKey step,
        IReadOnlyList<string> path,
        ITypeSymbol type,
        PropertyMappingModel value,
        List<SpecificationValueFact> values,
        out SpecificationValueKey? key,
        out string? reason) =>
        TryAddAt(
            context,
            evidenceFor,
            valueEvidence,
            step,
            path,
            type,
            value,
            values,
            TypeReference,
            out key,
            out reason);

    /// <summary>
    /// Adds one exact Stage query argument or result value at its semantic query path.
    /// </summary>
    /// <param name="context">The analyzed application projects.</param>
    /// <param name="evidenceFor">The exact evidence factory.</param>
    /// <param name="valueEvidence">Value-expression evidence by legacy model reference.</param>
    /// <param name="step">The owning neutral step.</param>
    /// <param name="path">The exact semantic query value path.</param>
    /// <param name="type">The exact formal parameter or read-model property type.</param>
    /// <param name="value">The recovered exact value.</param>
    /// <param name="values">The atomic value fact buffer.</param>
    /// <param name="key">The value key when successful.</param>
    /// <param name="reason">The blocking reason when unsuccessful.</param>
    /// <returns><see langword="true"/> when the value fact was added.</returns>
    public static bool TryAddQueryAt(
        DotNetAnalysisContext context,
        Func<Location, string?, Evidence> evidenceFor,
        IReadOnlyDictionary<PropertyMappingModel, Location> valueEvidence,
        SpecificationStepKey step,
        IReadOnlyList<string> path,
        ITypeSymbol type,
        PropertyMappingModel value,
        List<SpecificationValueFact> values,
        out SpecificationValueKey? key,
        out string? reason)
    {
        if (value.Source is not LiteralSource literal || !QueryValueMatches(literal.Value, type))
        {
            key = null;
            reason = $"value '{string.Join('/', path)}' does not exactly match its non-nullable scalar query type";
            return false;
        }

        return TryAddAt(
            context,
            evidenceFor,
            valueEvidence,
            step,
            path,
            type,
            value,
            values,
            QueryTypeReference,
            out key,
            out reason);
    }

    static bool TryAddAt(
        DotNetAnalysisContext context,
        Func<Location, string?, Evidence> evidenceFor,
        IReadOnlyDictionary<PropertyMappingModel, Location> valueEvidence,
        SpecificationStepKey step,
        IReadOnlyList<string> path,
        ITypeSymbol type,
        PropertyMappingModel value,
        List<SpecificationValueFact> values,
        Func<ITypeSymbol, DotNetAnalysisContext, TypeReferenceDefinition> typeReference,
        out SpecificationValueKey? key,
        out string? reason)
    {
        key = null;
        reason = null;
        if (value.Source is not LiteralSource literal || !valueEvidence.TryGetValue(value, out var source))
        {
            reason = $"value '{string.Join('/', path)}' is not an exact authored literal";
            return false;
        }

        if (!TryValue(literal.Value, out var kind, out var scalar))
        {
            reason = $"value '{string.Join('/', path)}' has an unsupported type or shape";
            return false;
        }

        key = new() { Step = step, Path = [.. path] };
        var pathText = string.Join('/', path);
        values.Add(new()
        {
            Id = FactId("value", step.Scenario.Scenario, $"{step.Index.ToString(CultureInfo.InvariantCulture)}:{string.Join(':', path)}"),
            Subject = new SubjectId { Value = $"{step.Scenario.Scenario.Value}/step/{step.Index.ToString(CultureInfo.InvariantCulture)}/value/{pathText}" },
            Evidence = evidenceFor(source, "The authored expression states this exact literal value"),
            Definition = new SpecificationValueDefinition
            {
                Key = key,
                Kind = kind,
                Type = typeReference(type, context),
                Scalar = scalar
            }
        });
        return true;
    }

    static bool QueryValueMatches(object? value, ITypeSymbol type)
    {
        if (value is null ||
            type.NullableAnnotation == NullableAnnotation.Annotated ||
            type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } ||
            CollectionElements.ElementOf(type) is not null)
        {
            return false;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return value is EnumValue enumeration &&
                type.GetMembers(enumeration.Member).OfType<IFieldSymbol>().Any(_ => _.HasConstantValue);
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.FindBase(WellKnownTypeNames.ConceptAs) is { TypeArguments: [var backing] })
        {
            return QueryValueMatches(value, backing);
        }

        return named.FullMetadataName() switch
        {
            "System.Guid" => value is string text && Guid.TryParse(text, out _),
            "System.String" => value is string,
            "System.Int32" => value is int,
            "System.Decimal" => value is decimal,
            "System.Boolean" => value is bool,
            "System.DateOnly" => value is string text && DateOnly.TryParse(text, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _),
            "System.DateTimeOffset" => value is string text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _),
            _ => false
        };
    }
}
