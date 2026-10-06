// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Events;

/// <summary>
/// Counts event construction and opaque production sites across every analyzed project, not just recovered slices.
/// </summary>
public static class EventProducers
{
    /// <summary>
    /// Gets the identity shared by a declaration and references to it in other compilations.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <returns>The assembly-qualified type identity.</returns>
    public static string IdentityOf(ITypeSymbol type) =>
        $"{type.ContainingAssembly.Identity.Name}:{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";

    /// <summary>
    /// Counts sites including reactors, helper code, clones and event-valued calls the analyzer cannot inspect.
    /// </summary>
    /// <param name="compilations">All projects analyzed for the application.</param>
    /// <param name="slices">All recovered slices, including commands sharing one aggregate construction.</param>
    /// <returns>The counts keyed by event type identity.</returns>
    /// <remarks>
    /// An opaque call or an existing event passed to a method is conservatively another site. Counting too many
    /// retains standalone syntax; counting too few could incorrectly move a shared persisted contract into a command.
    /// </remarks>
    public static IReadOnlyDictionary<string, int> Across(IEnumerable<Compilation> compilations, IEnumerable<SliceModel> slices)
    {
        var projects = compilations.ToList();
        var specifications = new SpecificationReader(new(projects), new());
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var compilation in projects)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var fixtures = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Where(declaration => model.GetDeclaredSymbol(declaration) is INamedTypeSymbol type &&
                        (specifications.IsSpecification(type) || IsTestFixture(type)))
                    .ToHashSet<SyntaxNode>();
                foreach (var node in root.DescendantNodes(descendIntoChildren: node => !fixtures.Contains(node) && !IsNameOf(node)))
                {
                    if (node is BaseObjectCreationExpressionSyntax or WithExpressionSyntax or InvocationExpressionSyntax)
                    {
                        Add(model.GetTypeInfo(node).Type, counts);
                    }

                    if (node is MethodDeclarationSyntax method && model.GetDeclaredSymbol(method) is IMethodSymbol symbol)
                    {
                        foreach (var eventType in HandlerBodies.EventTypesIn(symbol.ReturnType))
                        {
                            var key = IdentityOf(eventType);
                            var constructs = method.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()
                                .Any(creation => model.GetTypeInfo(creation).Type is { } constructed && IdentityOf(constructed) == key);
                            if (!constructs)
                            {
                                Add(eventType, counts);
                            }
                        }
                    }

                    if (node is InvocationExpressionSyntax invocation && !IsNameOf(invocation))
                    {
                        foreach (var argument in invocation.ArgumentList.Arguments.Where(_ => _.Expression is not BaseObjectCreationExpressionSyntax))
                        {
                            Add(model.GetTypeInfo(argument.Expression).Type, counts);
                        }
                    }
                }
            }
        }

        foreach (var group in slices.SelectMany(_ => _.Commands).SelectMany(_ => _.Produces)
            .Where(_ => _.EventTypeIdentity is not null).GroupBy(_ => _.EventTypeIdentity!, StringComparer.Ordinal))
        {
            counts[group.Key] = Math.Max(counts.GetValueOrDefault(group.Key), group.Count());
        }

        return counts;
    }

    static bool IsNameOf(SyntaxNode node) =>
        node is InvocationExpressionSyntax { Expression: IdentifierNameSyntax name } && name.Identifier.ValueText == "nameof";

    static bool IsTestFixture(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.FullMetadataName() == "Cratis.Specifications.Specification" ||
                current.GetMembers().OfType<IMethodSymbol>().Any(method => method.GetAttributes().Any(attribute =>
                    attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == "Xunit")))
            {
                return true;
            }
        }

        return false;
    }

    static void Add(ITypeSymbol? type, Dictionary<string, int> counts)
    {
        if (type is not null && EventReader.IsEvent(type))
        {
            var key = IdentityOf(type);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }
    }
}
