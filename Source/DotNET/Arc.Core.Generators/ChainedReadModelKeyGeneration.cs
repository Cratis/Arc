// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators;

/// <summary>Emits only relationship properties named by chained lookup declarations.</summary>
internal static class ChainedReadModelKeyGeneration
{
    const string Registry = "Cratis.Arc.Chronicle.ReadModels.ChainedReadModelKeys";
    static readonly SymbolDisplayFormat _format = SymbolDisplayFormat.FullyQualifiedFormat
        .RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>Renders registrations for the relationship properties referenced by discovered commands and validators.</summary>
    /// <param name="source">The module initializer being rendered.</param>
    /// <param name="commands">The discovered command types.</param>
    /// <param name="validators">The discovered validator types.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>Whether any relationship registrations were found.</returns>
    internal static bool Render(StringBuilder source, INamedTypeSymbol[] commands, INamedTypeSymbol[] validators, Compilation compilation)
    {
        if (compilation.GetTypeByMetadataName(Registry) is null) return false;
        var references = new Dictionary<INamedTypeSymbol, HashSet<string>>(SymbolEqualityComparer.Default);
        foreach (var member in commands.SelectMany(Members).Concat(validators.SelectMany(validator => validator.InstanceConstructors))) CollectReferences(member, references);
        foreach (var reference in references.OrderBy(pair => pair.Key.ToDisplayString(_format), StringComparer.Ordinal))
        {
            RenderModel(source, reference.Key, reference.Value, compilation);
        }
        return references.Count > 0;
    }

    static void CollectReferences(IMethodSymbol member, Dictionary<INamedTypeSymbol, HashSet<string>> references)
    {
        foreach (var target in member.Parameters)
        {
            if (Reference(member, target) is not { } reference) continue;
            if (!references.TryGetValue(reference.Model, out var names)) references.Add(reference.Model, names = []);
            names.Add(reference.Property);
        }
    }

    static (INamedTypeSymbol Model, string Property)? Reference(IMethodSymbol member, IParameterSymbol target)
    {
        var lookup = Lookup(target);
        if (lookup?.ConstructorArguments.Length != 2) return null;
        if (lookup.ConstructorArguments[0].Value is not string name || lookup.ConstructorArguments[1].Value is not string property) return null;
        return member.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Type is INamedTypeSymbol model ? (model, property) : null;
    }

    static IEnumerable<IMethodSymbol> Members(INamedTypeSymbol command)
    {
        for (var current = command; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>().Where(method => method.Name.Equals("Handle", StringComparison.Ordinal) || method.Name.Equals("Provide", StringComparison.Ordinal))) yield return method;
        }
    }

    static AttributeData? Lookup(IParameterSymbol parameter)
    {
        for (var current = parameter; current is not null; current = Inherited(current))
        {
            if (current.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "Cratis.Arc.Commands.LookUpByAttribute") is { } lookup) return lookup;
        }
        return null;
    }

    static IParameterSymbol? Inherited(IParameterSymbol parameter) => parameter.ContainingSymbol is IMethodSymbol { OverriddenMethod: { } inherited }
        ? inherited.Parameters[parameter.Ordinal] : null;

    static IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol model)
    {
        for (var current = model; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>()) yield return property;
        }
    }

    static void RenderModel(StringBuilder source, INamedTypeSymbol model, HashSet<string> names, Compilation compilation)
    {
        if (!TypeNaming.CanBeNamed(model, compilation)) return;
        var type = model.ToDisplayString(_format);
        var properties = Properties(model).GroupBy(property => property.Name, StringComparer.Ordinal).Select(group => group.First())
            .Where(property => names.Contains(property.Name) && !property.IsStatic && !property.IsIndexer && property.GetMethod?.DeclaredAccessibility == Accessibility.Public && TypeNaming.CanBeNamed(property.Type, compilation)).ToArray();
        if (properties.Length == 0) return;
        source.Append("        global::").Append(Registry).Append(".RegisterModel(typeof(").Append(type).Append(')');
        foreach (var property in properties)
        {
            source.Append(", new global::Cratis.Arc.Chronicle.ReadModels.CommandReadModelKeyProperty(")
                .Append(SymbolDisplay.FormatLiteral(property.Name, quote: true)).Append(", typeof(")
                .Append(property.Type.ToDisplayString(_format)).Append("), static instance => ((").Append(type).Append(")instance).@")
                .Append(property.Name).Append(')');
        }
        source.AppendLine(");");
    }
}
