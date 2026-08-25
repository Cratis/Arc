// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Creates deterministic neutral specification identities, types, values, and source-shape checks.
/// </summary>
internal static class ArcSpecificationFacts
{
    /// <summary>
    /// Creates an ordered step key.
    /// </summary>
    /// <param name="scenario">The scenario subject.</param>
    /// <param name="index">The authored step index.</param>
    /// <returns>The step key.</returns>
    public static SpecificationStepKey StepKey(SubjectId scenario, int index) => new()
    {
        Scenario = new() { Scenario = scenario },
        Index = index
    };

    /// <summary>
    /// Creates the source subject identity of an ordered step.
    /// </summary>
    /// <param name="scenario">The scenario subject.</param>
    /// <param name="index">The authored step index.</param>
    /// <returns>The step subject.</returns>
    public static SubjectId StepSubject(SubjectId scenario, int index) => new()
    {
        Value = $"{scenario.Value}/step/{index.ToString(CultureInfo.InvariantCulture)}"
    };

    /// <summary>
    /// Maps a legacy Arc specification state kind to its neutral counterpart.
    /// </summary>
    /// <param name="kind">The legacy kind.</param>
    /// <returns>The neutral kind.</returns>
    public static SpecificationStepKind Kind(SpecificationStateKind kind) => kind switch
    {
        SpecificationStateKind.Event => SpecificationStepKind.Event,
        SpecificationStateKind.ReadModel => SpecificationStepKind.ReadModel,
        SpecificationStateKind.Command => SpecificationStepKind.Command,
        _ => SpecificationStepKind.Unknown
    };

    /// <summary>
    /// Maps a neutral specification step to the referenced artifact kind.
    /// </summary>
    /// <param name="kind">The neutral step kind.</param>
    /// <returns>The artifact kind.</returns>
    public static ArtifactKind ArtifactKindFor(SpecificationStepKind kind) => kind switch
    {
        SpecificationStepKind.Event => ArtifactKind.Event,
        SpecificationStepKind.ReadModel => ArtifactKind.ReadModel,
        SpecificationStepKind.Command => ArtifactKind.Command,
        _ => ArtifactKind.Unknown
    };

    /// <summary>
    /// Converts one exact Arc literal to its neutral shape and canonical scalar.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="kind">The neutral value kind.</param>
    /// <param name="scalar">The canonical scalar.</param>
    /// <returns><see langword="true"/> when the literal is supported.</returns>
    public static bool TryValue(object? value, out SpecificationValueKind kind, out string? scalar)
    {
        (kind, scalar) = value switch
        {
            null => (SpecificationValueKind.Null, null),
            bool boolean => (SpecificationValueKind.Boolean, boolean ? "true" : "false"),
            string text => (SpecificationValueKind.Text, text),
            EnumValue enumeration => (SpecificationValueKind.Text, enumeration.Member),
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                (SpecificationValueKind.Number, Convert.ToString(value, CultureInfo.InvariantCulture)),
            _ => (SpecificationValueKind.Unknown, null)
        };
        return kind != SpecificationValueKind.Unknown;
    }

    /// <summary>
    /// Creates a neutral type reference from an exact Roslyn type.
    /// </summary>
    /// <param name="type">The exact Roslyn type.</param>
    /// <param name="context">The analyzed application context.</param>
    /// <returns>The neutral type reference.</returns>
    public static TypeReferenceDefinition TypeReference(ITypeSymbol type, DotNetAnalysisContext context) => new()
    {
        Name = type.SpecialType switch
        {
            SpecialType.System_String => "String",
            SpecialType.System_Boolean => "Bool",
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 => "Int",
            SpecialType.System_Decimal or SpecialType.System_Double or SpecialType.System_Single => "Decimal",
            _ => type.Name
        },
        Subject = type is INamedTypeSymbol named ? context.SubjectForType(named) : null
    };

    /// <summary>
    /// Creates a stable adapter-qualified fact identity.
    /// </summary>
    /// <param name="kind">The fact kind.</param>
    /// <param name="subject">The fact subject.</param>
    /// <param name="suffix">The optional identity suffix.</param>
    /// <returns>The fact identity.</returns>
    public static FactId FactId(string kind, SubjectId subject, string? suffix = null) => new()
    {
        Value = suffix is null
            ? $"{AdapterId(kind)}:{subject.Value}"
            : $"{AdapterId(kind)}:{subject.Value}:{suffix}"
    };

    /// <summary>
    /// Determines whether every required construction value survived legacy analysis.
    /// </summary>
    /// <param name="artifact">The exact constructed artifact.</param>
    /// <param name="valueCount">The number of exact values retained.</param>
    /// <returns><see langword="true"/> when no required constructor value was omitted.</returns>
    public static bool HasEveryRequiredConstructionValue(INamedTypeSymbol artifact, int valueCount)
    {
        var required = artifact.InstanceConstructors
            .Where(constructor => !constructor.IsStatic)
            .Select(constructor => constructor.Parameters.Count(parameter => !parameter.HasExplicitDefaultValue))
            .DefaultIfEmpty()
            .Max();
        return valueCount >= required;
    }

    /// <summary>
    /// Determines whether an expected event assertion contains predicate values the legacy analyzer did not retain.
    /// </summary>
    /// <param name="specification">The recovered specification.</param>
    /// <param name="evidence">The exact scenario evidence.</param>
    /// <returns><see langword="true"/> when predicate values would be lost.</returns>
    public static bool HasUnrepresentedEventPredicate(
        SpecificationModel specification,
        SpecificationScenarioEvidence evidence) =>
        specification.Then
            .Where(state => state.Kind == SpecificationStateKind.Event)
            .Select(state => evidence.States[state].Source)
            .Any(location => location.SourceTree!.GetRoot().FindNode(location.SourceSpan)
                .DescendantNodesAndSelf()
                .OfType<LambdaExpressionSyntax>()
                .Any());

    static string AdapterId(string kind) => $"cratis.arc.specifications:{kind}";
}
