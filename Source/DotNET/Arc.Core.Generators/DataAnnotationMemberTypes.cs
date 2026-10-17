// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators;

/// <summary>
/// Renders complete annotation metadata for types and queries with annotated concepts.
/// </summary>
internal static class DataAnnotationMemberTypes
{
    const string Registry = "global::Cratis.Arc.Validation.DataAnnotationMembers";
    const string Member = "global::Cratis.Arc.Validation.DataAnnotationMember";
    const string Attribute = "global::System.ComponentModel.DataAnnotations.ValidationAttribute";

    /// <summary>
    /// Gets complete annotated property registrations for reachable source types with annotated concepts.
    /// </summary>
    /// <param name="roots">The command and query argument roots.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated annotation registrations.</returns>
    internal static IEnumerable<ModelGraphWalker> For(IEnumerable<ITypeSymbol> roots, Compilation compilation, CancellationToken cancellationToken)
    {
        if (compilation.GetTypeByMetadataName("Cratis.Arc.Validation.DataAnnotationMembers") is null)
        {
            yield break;
        }

        foreach (var type in ModelGraphWalkerTypes.ReachableTypes(roots, compilation, cancellationToken))
        {
            var hierarchy = Properties(type).ToArray();
            var properties = hierarchy.Where(property =>
                property.DeclaredAccessibility == Accessibility.Public && !property.IsStatic &&
                property.Parameters.Length == 0 && property.GetMethod is not null && HasValidation(property)).ToArray();
            if (!properties.Any(property => IsConcept(property.Type)) ||
                !TypeNaming.CanBeNamed(type, compilation) || type.IsAbstract || type.IsStatic ||
                properties.Any(property => !ModelGraphWalkerTypes.CanRead(property, compilation) ||
                    hierarchy.Count(candidate => candidate.MetadataName == property.MetadataName) > 1))
            {
                continue;
            }

            var name = TypeName(type);
            var members = properties.Select(property => RenderMember(
                property,
                Read($"(({name})instance).@{property.Name}", property.Type),
                $"typeof({TypeName(property.ContainingType)}).GetProperty({Literal(property.Name)}, global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.DeclaredOnly)!"));
            yield return new(name, $"        {Registry}.Register(typeof({name}), new {Member}[] {{ {string.Join(", ", members)} }}, static () => {Attributes($"typeof({name})")});");
        }
    }

    /// <summary>
    /// Renders all annotated parameters under a query's identity when any annotated parameter is a concept.
    /// </summary>
    /// <param name="readModel">The read model used to identify the query.</param>
    /// <param name="method">The query method.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>The registration, or an empty string when complete, safe metadata cannot be generated.</returns>
    internal static string ForQuery(INamedTypeSymbol readModel, IMethodSymbol method, Compilation compilation)
    {
        var parameters = method.Parameters.Where(HasValidation).ToArray();
        if (!parameters.Any(parameter => IsConcept(parameter.Type)) || ModelGraphWalkerTypes.IsFlagged(method) || !CanResolve(method) ||
            !TypeNaming.CanBeNamed(readModel, compilation) || !TypeNaming.CanBeNamed(method.ContainingType, compilation) ||
            method.Parameters.Any(parameter => parameter.RefKind != RefKind.None || ModelGraphWalkerTypes.IsFlagged(parameter) ||
                !TypeNaming.CanBeNamed(parameter.Type, compilation)))
        {
            return string.Empty;
        }

        var name = TypeName(method.ContainingType);
        var signature = string.Join(", ", method.Parameters.Select(parameter => $"typeof({TypeName(parameter.Type)})"));
        const string flags = "global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static";
        var reflectedMethod = $"typeof({name}).GetMethod({Literal(method.Name)}, {flags}, null, new global::System.Type[] {{ {signature} }}, null)!";
        var members = parameters.Select(parameter => RenderMember(
            parameter,
            IsConcept(parameter.Type) ? $"instance is {TypeName(parameter.Type)} concept ? (object?)concept.Value : null" : "instance",
            $"{reflectedMethod}.GetParameters()[{parameter.Ordinal}]"));
        var identity = Literal($"{readModel.ToDisplayString()}.{method.Name}");
        return $"        {Registry}.RegisterQueryParameters({identity}, new {Member}[] {{ {string.Join(", ", members)} }});";
    }

    static IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                yield return property;
            }
        }
    }

    static bool CanResolve(IMethodSymbol method) => method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>()
        .Count(candidate => candidate.IsStatic && candidate.Parameters.All(parameter => parameter.RefKind == RefKind.None) &&
            candidate.Parameters.Select(parameter => parameter.Type).SequenceEqual(method.Parameters.Select(parameter => parameter.Type), SymbolEqualityComparer.Default)) == 1;

    static string Read(string access, ITypeSymbol type) => IsConcept(type) ? $"{access} is {{ }} concept ? (object?)concept.Value : null" : access;

    static string RenderMember(ISymbol symbol, string read, string attributeSource)
    {
        var display = $"global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::System.ComponentModel.DataAnnotations.DisplayAttribute>({attributeSource}, true)?.GetName() ?? {Literal(symbol.Name)}";
        return $"new {Member}({Literal(symbol.Name)}, {display}, static instance => {read}, static () => {Attributes(attributeSource)})";
    }

    static string Attributes(string source) =>
        $"global::System.Linq.Enumerable.ToArray(global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<{Attribute}>({source}, true))";

    static bool HasValidation(ISymbol symbol)
    {
        if (symbol.GetAttributes().Any(attribute => HasBase(attribute.AttributeClass, "System.ComponentModel.DataAnnotations.ValidationAttribute")))
        {
            return true;
        }

        return symbol is IPropertySymbol { OverriddenProperty: { } inherited } && HasValidation(inherited);
    }

    static bool IsConcept(ITypeSymbol type) => HasBase(type as INamedTypeSymbol, "Cratis.Concepts.ConceptAs`1");

    static bool HasBase(INamedTypeSymbol? type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if ($"{current.OriginalDefinition.ContainingNamespace.ToDisplayString()}.{current.OriginalDefinition.MetadataName}" == name)
            {
                return true;
            }
        }

        return false;
    }

    static string TypeName(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);
}
