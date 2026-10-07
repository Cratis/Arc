// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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
        var recoveredSlices = slices.ToList();
        var commands = recoveredSlices.SelectMany(slice => slice.Commands.SelectMany(command => command.Produces
            .Where(production => production.EventTypeIdentity is not null)
            .Select(production => (Type: $"{slice.Namespace}.{command.Name}", Event: production.EventTypeIdentity!))))
            .ToHashSet();
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
                        foreach (var eventType in CarriedEvents(symbol.ReturnType, includeReturnWrappers: true))
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

                    foreach (var expression in EscapingValues(node, model))
                    {
                        var type = model.GetTypeInfo(expression);

                        // Direct construction, clones and event-valued calls are already counted above.
                        if (expression is BaseObjectCreationExpressionSyntax or WithExpressionSyntax or InvocationExpressionSyntax &&
                            type.Type is { } direct && EventReader.IsEvent(direct))
                        {
                            continue;
                        }

                        foreach (var eventType in CarriedEvents(type.Type ?? type.ConvertedType))
                        {
                            var key = IdentityOf(eventType);
                            var inProducingCommand = node.Ancestors().OfType<TypeDeclarationSyntax>().Any(declaration =>
                                model.GetDeclaredSymbol(declaration) is INamedTypeSymbol enclosing && commands.Contains((enclosing.ToDisplayString(), key)));
                            if (!inProducingCommand)
                            {
                                Add(eventType, counts);
                            }
                        }
                    }
                }
            }
        }

        foreach (var group in recoveredSlices.SelectMany(_ => _.Commands).SelectMany(_ => _.Produces)
            .Where(_ => _.EventTypeIdentity is not null).GroupBy(_ => _.EventTypeIdentity!, StringComparer.Ordinal))
        {
            counts[group.Key] = Math.Max(counts.GetValueOrDefault(group.Key), group.Count());
        }

        return counts;
    }

    static IEnumerable<ExpressionSyntax> EscapingValues(SyntaxNode node, SemanticModel model) => node switch
    {
        InvocationExpressionSyntax invocation when !IsNameOf(invocation) => invocation.ArgumentList.Arguments.Select(argument => argument.Expression),
        BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments } => arguments.Arguments.Select(argument => argument.Expression),
        ConstructorInitializerSyntax initializer => initializer.ArgumentList.Arguments.Select(argument => argument.Expression),
        InitializerExpressionSyntax initializer when initializer.IsKind(SyntaxKind.ArrayInitializerExpression) ||
            initializer.IsKind(SyntaxKind.CollectionInitializerExpression) || initializer.IsKind(SyntaxKind.ComplexElementInitializerExpression) => initializer.Expressions,
        CollectionExpressionSyntax collection => collection.Elements.Select(element => element switch
        {
            ExpressionElementSyntax value => value.Expression,
            SpreadElementSyntax spread => spread.Expression,
            _ => null
        }).OfType<ExpressionSyntax>(),
        ReturnStatementSyntax { Expression: { } expression } => [expression],
        YieldStatementSyntax { Expression: { } expression } => [expression],
        ArrowExpressionClauseSyntax arrow => [arrow.Expression],
        LambdaExpressionSyntax { Body: ExpressionSyntax expression } => [expression],
        AssignmentExpressionSyntax assignment when IsNonLocalTarget(model.GetOperation(assignment.Left)) => [assignment.Right],
        EqualsValueClauseSyntax value when value.Parent is PropertyDeclarationSyntax ||
            value.Parent is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax } => [value.Value],
        _ => []
    };

    static bool IsNonLocalTarget(IOperation? target) => target is IFieldReferenceOperation or IPropertyReferenceOperation or IArrayElementReferenceOperation ||
        (target is ITupleOperation tuple && tuple.Elements.Any(IsNonLocalTarget));

    static IEnumerable<INamedTypeSymbol> CarriedEvents(ITypeSymbol? type, bool includeReturnWrappers = false) =>
        CarriedEvents(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), includeReturnWrappers);

    static IEnumerable<INamedTypeSymbol> CarriedEvents(ITypeSymbol? type, HashSet<ITypeSymbol> visited, bool includeReturnWrappers)
    {
        if (type is null || !visited.Add(type))
        {
            yield break;
        }

        if (type is IArrayTypeSymbol array)
        {
            foreach (var eventType in CarriedEvents(array.ElementType, visited, includeReturnWrappers))
            {
                yield return eventType;
            }
        }
        else if (type is INamedTypeSymbol named && named.TypeKind != TypeKind.Delegate)
        {
            if (EventReader.IsEvent(named))
            {
                yield return named;
                yield break;
            }

            // Return signatures also promise events through wrappers such as Task and Result, but a
            // delegate mentioning an event describes consumption, not a returned event instance.
            var components = named.IsTupleType ? named.TupleElements.Select(element => element.Type) :
                named.AllInterfaces.Prepend(named).Where(candidate => candidate.Is("System.Collections.Generic.IEnumerable`1"))
                    .SelectMany(candidate => candidate.TypeArguments).Concat(includeReturnWrappers ? named.TypeArguments : []);
            foreach (var component in components)
            {
                foreach (var eventType in CarriedEvents(component, visited, includeReturnWrappers))
                {
                    yield return eventType;
                }
            }
        }
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
