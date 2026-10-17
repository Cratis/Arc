// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators;

/// <summary>
/// Renders annotated member registrations with typed readers that unwrap concepts without reflection.
/// </summary>
internal static class DataAnnotationMemberTypes
{
    const string Registry = "global::Cratis.Arc.Validation.DataAnnotationMembers";
    const string Member = "global::Cratis.Arc.Validation.DataAnnotationMember";
    const string Attribute = "global::System.ComponentModel.DataAnnotations.ValidationAttribute";

    /// <summary>
    /// Gets annotated property registrations for source types reachable from the given roots.
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
            var properties = ModelGraphWalkerTypes.AnnotatedProperties(type, compilation, cancellationToken);
            var annotated = properties?.Where(HasValidation).ToArray();
            if (annotated is not { Length: > 0 })
            {
                continue;
            }

            var name = TypeName(type);
            var members = annotated.Select(property => RenderMember(
                property,
                Read($"(({name})instance).@{property.Name}", property.Type),
                $"typeof({name}).GetProperty({Literal(property.Name)})!"));
            yield return new(name, $"        {Registry}.Register(typeof({name}), new {Member}[] {{ {string.Join(", ", members)} }});");
        }
    }

    /// <summary>
    /// Renders annotated parameters under a model-bound query's identity.
    /// </summary>
    /// <param name="readModel">The read model used to identify the query.</param>
    /// <param name="method">The query method.</param>
    /// <returns>The registration, or an empty string if no parameter is annotated.</returns>
    internal static string ForQuery(INamedTypeSymbol readModel, IMethodSymbol method)
    {
        var parameters = method.Parameters.Where(HasValidation).ToArray();
        if (parameters.Length == 0)
        {
            return string.Empty;
        }

        var name = TypeName(method.ContainingType);
        var signature = string.Join(", ", method.Parameters.Select(parameter => $"typeof({TypeName(parameter.Type)})"));
        var reflectedMethod = $"typeof({name}).GetMethod({Literal(method.Name)}, new global::System.Type[] {{ {signature} }})!";
        var members = parameters.Select(parameter => RenderMember(
            parameter,
            IsConcept(parameter.Type) ? $"instance is {TypeName(parameter.Type)} concept ? (object?)concept.Value : null" : "instance",
            $"{reflectedMethod}.GetParameters()[{parameter.Ordinal}]"));
        var identity = Literal($"{readModel.ToDisplayString()}.{method.Name}");
        return $"        {Registry}.RegisterQueryParameters({identity}, new {Member}[] {{ {string.Join(", ", members)} }});";
    }

    static string Read(string access, ITypeSymbol type) => IsConcept(type) ? $"{access} is {{ }} concept ? (object?)concept.Value : null" : access;

    static string RenderMember(ISymbol symbol, string read, string attributeSource)
    {
        var attributes = $"global::System.Linq.Enumerable.ToArray(global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<{Attribute}>({attributeSource}, true))";
        return $"new {Member}({Literal(symbol.Name)}, {Literal(DisplayName(symbol))}, static instance => {read}, static () => {attributes})";
    }

    static string DisplayName(ISymbol symbol) => symbol.GetAttributes()
        .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.DataAnnotations.DisplayAttribute")?
        .NamedArguments.FirstOrDefault(argument => argument.Key == "Name").Value.Value as string ?? symbol.Name;

    static bool HasValidation(ISymbol symbol) => symbol.GetAttributes().Any(attribute =>
        HasBase(attribute.AttributeClass, "System.ComponentModel.DataAnnotations.ValidationAttribute"));

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
