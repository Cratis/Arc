// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators;

/// <summary>
/// Renders complete annotation metadata for types with annotated concepts and query parameters.
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
            var properties = hierarchy.Where(IsValidatedProperty).ToArray();
            if (!CanRegister(type, properties, hierarchy, compilation))
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
    /// Renders all annotated parameters under a query's identity.
    /// </summary>
    /// <param name="readModel">The read model used to identify the query.</param>
    /// <param name="method">The query method.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>The registration, or an empty string when complete, safe metadata cannot be generated.</returns>
    internal static string ForQuery(INamedTypeSymbol readModel, IMethodSymbol method, Compilation compilation)
    {
        var parameters = method.Parameters.Where(HasValidation).ToArray();
        if (parameters.Length == 0 || !CanRegisterQuery(readModel, method, compilation))
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

    static bool IsValidatedProperty(IPropertySymbol property)
    {
        if (property.DeclaredAccessibility != Accessibility.Public || property.IsStatic)
        {
            return false;
        }

        return HasReadableGetter(property) && HasValidation(property);
    }

    static bool HasReadableGetter(IPropertySymbol property) => property.Parameters.Length == 0 && property.GetMethod is not null;

    static bool CanRegister(INamedTypeSymbol type, IPropertySymbol[] properties, IPropertySymbol[] hierarchy, Compilation compilation)
    {
        if (!properties.Any(property => IsConcept(property.Type)) || !TypeNaming.CanBeNamed(type, compilation))
        {
            return false;
        }

        if (type.IsAbstract || type.IsStatic)
        {
            return false;
        }

        return !properties.Any(property => !ModelGraphWalkerTypes.CanRead(property, compilation) || IsAmbiguous(property, hierarchy));
    }

    static bool IsAmbiguous(IPropertySymbol property, IPropertySymbol[] hierarchy) =>
        hierarchy.Count(candidate => candidate.MetadataName == property.MetadataName) > 1;

    static bool CanRegisterQuery(INamedTypeSymbol readModel, IMethodSymbol method, Compilation compilation)
    {
        if (ModelGraphWalkerTypes.IsFlagged(method) || !CanResolve(method))
        {
            return false;
        }

        if (!TypeNaming.CanBeNamed(readModel, compilation) || !TypeNaming.CanBeNamed(method.ContainingType, compilation))
        {
            return false;
        }

        return !method.Parameters.Any(parameter => !CanRegisterQueryParameter(parameter, compilation));
    }

    static bool CanRegisterQueryParameter(IParameterSymbol parameter, Compilation compilation)
    {
        if (parameter.RefKind != RefKind.None || ModelGraphWalkerTypes.IsFlagged(parameter))
        {
            return false;
        }

        return TypeNaming.CanBeNamed(parameter.Type, compilation);
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
        .Count(candidate => candidate.IsStatic && MatchesReflectionParameters(candidate, method)) == 1;

    static bool MatchesReflectionParameters(IMethodSymbol candidate, IMethodSymbol method) =>
        candidate.Parameters.All(parameter => parameter.RefKind == RefKind.None) &&
        candidate.Parameters.Select(parameter => parameter.Type).SequenceEqual(method.Parameters.Select(parameter => parameter.Type), SymbolEqualityComparer.Default);

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
