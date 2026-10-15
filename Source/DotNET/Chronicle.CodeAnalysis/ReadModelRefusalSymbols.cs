// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Recognizes only source-visible admission refusals; unseen registration/delivery remains runtime-validated.
/// </summary>
internal static class ReadModelRefusalSymbols
{
    /// <summary>
    /// Describes a proven refusal of an automatically injected model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="compilation">The compilation.</param>
    /// <param name="backings">Source-declared backing definitions.</param>
    /// <returns>The refusal and fix, or null when no refusal is statically proven.</returns>
    internal static (string Reason, string Fix)? Refusal(INamedTypeSymbol model, Compilation compilation, INamedTypeSymbol[] backings)
    {
        var attributes = Attributes(model).ToArray();
        if (attributes.Any(attribute => IsProjectionAttribute(attribute, "Join") || IsProjectionAttribute(attribute, "Children")))
        {
            return ("with joins or children", "Read the parts as separate models");
        }
        if (attributes.Any(EventPropertyKey))
        {
            return ("keyed by an event property", "Append a fact to the entity it is keyed by, or use a constraint");
        }
        if (ChronicleStateSymbols.ModelKey(model, compilation) is { Type: { } key } && UnsupportedKey(key, compilation))
        {
            return ("with an unsupported key type", $"Give {model.Name} a string or Guid Id");
        }
        foreach (var backing in backings.Where(backing => backing.AllInterfaces.Any(contract =>
                     contract.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], model))))
        {
            if (backing.AllInterfaces.Any(contract => RawGuidResponseAnalysis.IsType(contract, "Cratis.Chronicle.Reducers.IReducerFor`1", compilation)))
            {
                if (!backing.InstanceConstructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.Length == 0))
                {
                    return ("through a reducer with constructor dependencies", "Remove constructor dependencies from the reducer");
                }
                if (Filtered(backing))
                {
                    return ("through a reducer that filters its events", "Remove the reducer's event filter or read an unfiltered projection");
                }
            }
            var refusal = FluentRefusal(backing, compilation);
            if (refusal is not null)
            {
                return refusal;
            }
        }
        return null;
    }

    static IEnumerable<AttributeData> Attributes(INamedTypeSymbol model) =>
        model.GetAttributes().Concat(RawGuidResponseAnalysis.PublicProperties(model).SelectMany(property => property.GetAttributes()))
            .Concat(model.InstanceConstructors.SelectMany(constructor => constructor.Parameters).SelectMany(parameter => parameter.GetAttributes()));

    static bool IsProjectionAttribute(AttributeData attribute, string prefix) =>
        attribute.AttributeClass is { } type && type.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections.ModelBound" &&
        type.Name.StartsWith(prefix, StringComparison.Ordinal);

    static bool EventPropertyKey(AttributeData attribute)
    {
        if (attribute.AttributeClass?.ContainingNamespace.ToDisplayString() != "Cratis.Chronicle.Projections.ModelBound")
        {
            return false;
        }
        var parameters = attribute.AttributeConstructor?.Parameters;
        return (parameters?.Where((parameter, index) =>
            (parameter.Name == "key" || parameter.Name == "parentKey") && index < attribute.ConstructorArguments.Length &&
            attribute.ConstructorArguments[index].Value is string { Length: > 0 }).Any() == true) ||
            attribute.NamedArguments.Any(argument => argument.Key == "ConstantKey" && argument.Value.Value is not null);
    }

    static bool UnsupportedKey(ITypeSymbol type, Compilation compilation)
    {
        // Chronicle unwraps concepts and EventSourceId to their primitive wire value.
        if (ChronicleStateSymbols.IsEventSourceId(type, compilation))
        {
            return false;
        }
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is INamedTypeSymbol named && RawGuidResponseAnalysis.IsType(named, "Cratis.Concepts.ConceptAs`1", compilation))
            {
                return UnsupportedKey(named.TypeArguments[0], compilation);
            }
        }
        return (type.SpecialType != SpecialType.None && type.SpecialType != SpecialType.System_String) ||
            type.TypeKind == TypeKind.Enum;
    }

    static bool Filtered(INamedTypeSymbol reducer)
    {
        for (var current = reducer; current is not null; current = current.BaseType)
        {
            if (current.GetAttributes().Any(attribute => attribute.AttributeClass is { } type &&
                ((type.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle" && type.Name == "FilterEventsByTagAttribute") ||
                 (type.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.EventSources" && type.Name == "FromEventSourceAttribute") ||
                 (type.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Events" && (type.Name == "EventSourceTypeAttribute" || type.Name == "EventStreamTypeAttribute")))))
            {
                return true;
            }
        }
        return false;
    }

    static (string Reason, string Fix)? FluentRefusal(INamedTypeSymbol backing, Compilation compilation)
    {
        foreach (var reference in backing.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax();
            var semantic = compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (semantic.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
                    (!method.ContainingNamespace.ToDisplayString().StartsWith("Cratis.Chronicle.Projections", StringComparison.Ordinal) &&
                     method.ContainingNamespace.ToDisplayString() != "Cratis.Chronicle.Keys"))
                {
                    continue;
                }
                if (method.Name.StartsWith("Join", StringComparison.Ordinal) || method.Name.StartsWith("Children", StringComparison.Ordinal))
                {
                    return ("with joins or children", "Read the parts as separate models");
                }
                if (method.Name == "UsingKey" || method.Name == "UsingParentKey")
                {
                    return ("keyed by an event property", "Append a fact to the entity it is keyed by, or use a constraint");
                }
            }
        }
        return null;
    }
}
