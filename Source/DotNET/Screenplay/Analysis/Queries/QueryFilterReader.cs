// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Recovers only caller-supplied equality filters the executable query contract can represent.
/// </summary>
/// <param name="types">The registry naming the read model's property types.</param>
/// <param name="models">The models resolving source evidence, if available.</param>
/// <param name="diagnostics">Where omitted parameters are reported.</param>
internal sealed class QueryFilterReader(TypeRegistry types, SemanticModels? models, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads proven filters in caller parameter order.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="key">The existing by parameter, if any.</param>
    /// <param name="location">Where omissions are reported.</param>
    /// <returns>The proven scalar equality filters.</returns>
    internal IEnumerable<PropertyModel> Read(IMethodSymbol method, IParameterSymbol? key, string location)
    {
        var collection = false;
        var returned = UnderlyingTypes.Of(QueryReturnTypes.Unwrap(method.ReturnType, ref collection));
        var evidence = EvidenceOf(method);
        var properties = collection ? returned.DeclaredProperties().ToList() : [];
        var ambiguousNames = AmbiguousNamesOf(method);

        foreach (var parameter in method.Parameters.Where(QueryReader.IsInput).Where(parameter => !SymbolEqualityComparer.Default.Equals(parameter, key)))
        {
            var filter = ambiguousNames.Contains(parameter.Name) ? null : ReadParameter(parameter, properties, evidence);
            if (filter is not null)
            {
                yield return filter;
            }
            else
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UnmappableQuery,
                    $"The query '{method.Name}' parameter '{parameter.Name}' is not a proven same-named scalar equality filter on the returned collection, so its filter was left out",
                    location);
            }
        }
    }

    static HashSet<string> AmbiguousNamesOf(IMethodSymbol method) => method.Parameters.Where(QueryReader.IsInput)
        .GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1)
        .Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    static bool HasNonNullDefault(IParameterSymbol parameter) => parameter.HasExplicitDefaultValue && parameter.ExplicitDefaultValue is not null;

    static bool SameType(ITypeSymbol left, ITypeSymbol right) => SymbolEqualityComparer.Default.Equals(UnderlyingTypes.Of(left), UnderlyingTypes.Of(right));

    static bool IsScalar(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;
        var underlying = UnderlyingTypes.Of(type, ref optional, ref collection);

        return !collection && IsScalarValue(underlying);
    }

    static bool IsScalarValue(ITypeSymbol type) => type.TypeKind == TypeKind.Enum || type.FindBase(WellKnownTypeNames.ConceptAs) is not null ||
        (type is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out _));

    static bool IsRepresentable(IParameterSymbol parameter, IPropertySymbol property)
    {
        if (!IsScalar(property.Type))
        {
            return false;
        }

        if (!IsScalar(parameter.Type))
        {
            return false;
        }

        if (HasNonNullDefault(parameter))
        {
            return false;
        }

        return SameType(parameter.Type, property.Type);
    }

    Evidence? EvidenceOf(IMethodSymbol method)
    {
        var declaration = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (declaration is null || models?.For(declaration.SyntaxTree) is not { } model)
        {
            return null;
        }

        return new(declaration, model, QueryPredicates.Of(declaration, model).ToList());
    }

    PropertyModel? ReadParameter(IParameterSymbol parameter, IEnumerable<IPropertySymbol> properties, Evidence? evidence)
    {
        var matching = properties.Where(property => string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        return matching.Count == 1 && evidence is not null ? Prove(parameter, matching[0], evidence) : null;
    }

    PropertyModel? Prove(IParameterSymbol parameter, IPropertySymbol property, Evidence evidence)
    {
        if (!IsRepresentable(parameter, property))
        {
            return null;
        }

        var proofs = evidence.Predicates.Select(predicate => new EqualityQueryFilter(evidence.Model, parameter))
            .Zip(evidence.Predicates, (proof, predicate) => proof.Matches(predicate, property) ? proof : null).OfType<EqualityQueryFilter>().ToList();
        if (proofs.Count != 1 || !proofs[0].HasOnlyProvenUses(evidence.Declaration))
        {
            return null;
        }

        return new(property.Name, types.Resolve(property.Type) with { IsOptional = proofs[0].Optional });
    }

    sealed record Evidence(MethodDeclarationSyntax Declaration, SemanticModel Model, IReadOnlyList<LambdaExpressionSyntax> Predicates);
}
