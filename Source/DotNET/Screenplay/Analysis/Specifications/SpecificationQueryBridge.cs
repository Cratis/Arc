// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Queries;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Recovers a generated query scenario as a specification of the slice it sits in.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="queries">The queries the application declares, which a scenario's call is matched against.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> a scenario left out is reported to.</param>
/// <remarks>
/// The scenario is read the same way the fact path reads it, and held to the same proof before it is stated: its
/// arguments and expected values in the formal order the application declares them, no two of them collapsing onto
/// one name, and a single required input that is the read model's own key. A scenario failing any of that is left
/// out and said so, rather than stated as a query the document would not run the same way.
/// </remarks>
internal sealed class SpecificationQueryBridge(
    SemanticModels models,
    IReadOnlyList<SpecificationQueryCatalog.Entry> queries,
    ScreenplayDiagnostics diagnostics)
{
    readonly SpecificationQueryReader _reader = new(models, queries);
    readonly ScreenplayNaming _naming = new();

    /// <summary>
    /// Reads a type as a generated query scenario.
    /// </summary>
    /// <param name="type">The type declaring the scenario.</param>
    /// <param name="placement">The placement of scenarios under slices.</param>
    /// <param name="read">The slice and specification, set only when the scenario was recovered.</param>
    /// <returns><see langword="true"/> when the type is written as a query scenario, whether recovered or left out.</returns>
    public bool TryRead(INamedTypeSymbol type, SpecificationPlacement placement, out (string Slice, SpecificationModel Specification)? read)
    {
        read = null;
        if (!_reader.TryRead(type, out var evidence, out var reason) || evidence is null)
        {
            return Refused(type, reason);
        }

        if (Unprovable(evidence) is { } unprovable)
        {
            return Refused(type, unprovable);
        }

        if (placement.SliceOf(type) is not { } slice)
        {
            return Refused(type, "no namespace above it declares a slice for it to specify");
        }

        read = (slice, Specification(SpecificationPlacement.NameOf(type, slice), evidence));
        return true;
    }

    static SpecificationModel Specification(string name, SpecificationQueryEvidence evidence) =>
        new(name, [], null, [], [])
        {
            Read = new(evidence.Query.Name, [.. evidence.Arguments], [.. evidence.Result])
        };

    static bool InFormalOrder(IEnumerable<string> declared, IReadOnlyList<PropertyMappingModel> stated) =>
        declared.SequenceEqual(stated.Select(value => value.Property), StringComparer.Ordinal);

    static bool IsNonNullableScalar(ITypeSymbol type) =>
        type.NullableAnnotation != NullableAnnotation.Annotated &&
        type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } &&
        CollectionElements.ElementOf(type) is null;

    bool Refused(INamedTypeSymbol type, string? reason)
    {
        if (reason is null)
        {
            return false;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnreadableSpecification,
            $"The scenario '{type.Name}' was left out because {reason}",
            type.ToDisplayString());

        return true;
    }

    string? Unprovable(SpecificationQueryEvidence evidence)
    {
        var inputs = evidence.Query.Parameters.Where(QueryReader.IsInput).ToArray();
        var properties = evidence.ReadModel.DeclaredProperties().ToArray();
        if (!InFormalOrder(inputs.Select(input => input.Name), evidence.Arguments) ||
            !InFormalOrder(properties.Select(property => property.Name), evidence.Result))
        {
            return "the query arguments or result values lost their exact formal declaration order";
        }

        if (!NamesAreUnique(inputs.Select(input => input.Name)) || !NamesAreUnique(properties.Select(property => property.Name)))
        {
            return "two query argument or result paths normalize to the same semantic property name";
        }

        if (!evidence.Arguments.Concat(evidence.Result).All(value => IsExact(value.Source)))
        {
            return "a query argument or result value is text the document cannot state exactly as it was authored";
        }

        return inputs is [var input] && IsKeyed(input, properties)
            ? null
            : "the required query input has no unique non-nullable scalar read-model key with the same normalized name and exact type";
    }

    /// <summary>
    /// Determines whether a value is written into the document exactly as the scenario states it.
    /// </summary>
    /// <param name="source">The value the scenario states.</param>
    /// <returns><see langword="true"/> when nothing about the value would be changed on its way into the document.</returns>
    /// <remarks>
    /// Text is written as a document string literal, which collapses whitespace and cannot hold a double quote or a
    /// backslash. A value that would be changed is not sanitized into a different one; the scenario is left out.
    /// </remarks>
    bool IsExact(MappingSourceModel source) => source switch
    {
        LiteralSource { Value: string text } => text.IsNormalized() &&
            !text.Contains('\\', StringComparison.Ordinal) &&
            string.Equals(_naming.ToStringLiteral(text), text, StringComparison.Ordinal),
        LiteralSource { Value: object?[] items } => items.All(item => IsExact(new LiteralSource(item))),
        _ => true
    };

    bool IsKeyed(IParameterSymbol input, IPropertySymbol[] properties) =>
        IsNonNullableScalar(input.Type) &&
        properties.Count(property =>
            string.Equals(_naming.ToPropertyName(property.Name), _naming.ToPropertyName(input.Name), StringComparison.Ordinal) &&
            SymbolEqualityComparer.IncludeNullability.Equals(property.Type, input.Type) &&
            IsNonNullableScalar(property.Type)) == 1;

    bool NamesAreUnique(IEnumerable<string> names)
    {
        var normalized = names.Select(_naming.ToPropertyName).ToArray();
        return normalized.Distinct(StringComparer.Ordinal).Count() == normalized.Length;
    }
}
