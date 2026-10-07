// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    /// Outside the producing command, any event-valued use other than a member read or pattern subject is another
    /// site. Aggregate behavior calls must all belong to that command's recovered production. Counting too many
    /// retains standalone syntax; counting too few could incorrectly move a shared persisted contract into a command.
    /// </remarks>
    public static IReadOnlyDictionary<string, int> Across(IEnumerable<Compilation> compilations, IEnumerable<SliceModel> slices)
    {
        var projects = compilations.ToList();

        return Across(projects, slices, new(projects));
    }

    /// <summary>
    /// Counts production sites through the application's shared semantic models.
    /// </summary>
    /// <param name="compilations">All projects analyzed for the application.</param>
    /// <param name="slices">All recovered slices.</param>
    /// <param name="models">The shared cached semantic models.</param>
    /// <returns>The counts keyed by event type identity.</returns>
    internal static IReadOnlyDictionary<string, int> Across(IEnumerable<Compilation> compilations, IEnumerable<SliceModel> slices, SemanticModels models)
    {
        var projects = compilations.ToList();
        var recoveredSlices = slices.ToList();
        var commands = recoveredSlices.SelectMany(slice => slice.Commands.SelectMany(command => command.Produces
            .Where(production => production.EventTypeIdentity is not null)
            .Select(production => (Type: $"{slice.Namespace}.{command.Name}", Event: production.EventTypeIdentity!))))
            .ToHashSet();
        var behaviors = RecoveredBehaviors(projects, commands, models);
        var specifications = new SpecificationReader(models, new());
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var compilation in projects)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var root = tree.GetRoot();
                if (!root.DescendantNodes().Any(IsCandidate))
                {
                    continue;
                }

                var model = models.For(tree)!;
                var fixtures = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Where(declaration => model.GetDeclaredSymbol(declaration) is INamedTypeSymbol type &&
                        (specifications.IsSpecification(type) || IsTestFixture(type)))
                    .ToHashSet<SyntaxNode>();
                foreach (var node in root.DescendantNodes(descendIntoChildren: node => !fixtures.Contains(node) && !IsNameOf(node)).Where(IsCandidate))
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

                    if (node is InvocationExpressionSyntax call && model.GetSymbolInfo(call).Symbol is IMethodSymbol called &&
                        behaviors.TryGetValue(MethodIdentity(called), out var productions))
                    {
                        foreach (var production in productions.Where(production => !InCommandHandler(call, model, production.Type)))
                        {
                            counts[production.Event] = counts.GetValueOrDefault(production.Event) + 1;
                        }
                    }

                    if (node is not ExpressionSyntax expression || IsReadOnlyUse(expression, model) ||
                        (expression.Parent is MemberAccessExpressionSyntax member && member.Name == expression) ||
                        (expression.Parent is MemberBindingExpressionSyntax binding && binding.Name == expression) ||
                        model.GetSymbolInfo(expression).Symbol is INamespaceOrTypeSymbol)
                    {
                        continue;
                    }

                    foreach (var eventType in CarriedEvents(model.GetTypeInfo(expression).Type)
                        .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
                    {
                        // Direct event construction, clones and calls are already counted above, even inside commands.
                        if (expression is BaseObjectCreationExpressionSyntax or WithExpressionSyntax or InvocationExpressionSyntax &&
                            EventReader.IsEvent(model.GetTypeInfo(expression).Type!))
                        {
                            continue;
                        }

                        var key = IdentityOf(eventType);
                        var enclosingDeclaration = expression.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                        var inProducingCommand = enclosingDeclaration is not null &&
                            model.GetDeclaredSymbol(enclosingDeclaration) is INamedTypeSymbol enclosing && commands.Contains((enclosing.ToDisplayString(), key));
                        if (!inProducingCommand)
                        {
                            Add(eventType, counts);
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

    static Dictionary<string, HashSet<(string Type, string Event)>> RecoveredBehaviors(
        IEnumerable<Compilation> projects, HashSet<(string Type, string Event)> commands, SemanticModels models)
    {
        var recovered = new Dictionary<string, HashSet<(string Type, string Event)>>(StringComparer.Ordinal);
        foreach (var compilation in projects)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var declarations = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText == CommandReader.HandleMethod).ToList();
                if (declarations.Count == 0)
                {
                    continue;
                }

                var model = models.For(tree)!;
                foreach (var declaration in declarations)
                {
                    if (model.GetDeclaredSymbol(declaration) is not { } handler || handler.Name != CommandReader.HandleMethod)
                    {
                        continue;
                    }

                    var productions = commands.Where(command => command.Type == handler.ContainingType.ToDisplayString()).ToList();
                    if (productions.Count == 0)
                    {
                        continue;
                    }

                    foreach (var body in HandlerBodies.Of(handler))
                    {
                        foreach (var behavior in AggregateRootBehaviors.ReachedFrom(body, model))
                        {
                            var behaviorModel = models.For(behavior.Body.SyntaxTree);
                            if (behaviorModel?.GetEnclosingSymbol(behavior.Body.SpanStart) is not IMethodSymbol method)
                            {
                                continue;
                            }

                            var events = behavior.Body.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>()
                                .Select(creation => behaviorModel.GetTypeInfo(creation).Type).OfType<INamedTypeSymbol>()
                                .Where(EventReader.IsEvent).Select(IdentityOf).ToHashSet(StringComparer.Ordinal);
                            var key = MethodIdentity(method);
                            if (!recovered.TryGetValue(key, out var owners))
                            {
                                owners = [];
                                recovered[key] = owners;
                            }

                            owners.UnionWith(productions.Where(production => events.Contains(production.Event)));
                        }
                    }
                }
            }
        }

        return recovered;
    }

    /// <summary>
    /// Skips syntax that cannot carry an event, retaining inferred values even when their tree never names its type.
    /// </summary>
    /// <param name="node">The syntax to inspect before acquiring its semantic model.</param>
    /// <returns>Whether the syntax may contribute a production site.</returns>
    static bool IsCandidate(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax => true,
        LiteralExpressionSyntax or PredefinedTypeSyntax => false,
        ExpressionSyntax => true,
        _ => false
    };

    static string MethodIdentity(IMethodSymbol method) =>
        $"{IdentityOf(method.ContainingType)}:{method.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";

    static bool InCommandHandler(SyntaxNode node, SemanticModel model, string command)
    {
        var declaration = node.Ancestors().FirstOrDefault(ancestor => ancestor is MethodDeclarationSyntax or
            AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax);

        return declaration is MethodDeclarationSyntax method && model.GetDeclaredSymbol(method) is { } handler && handler.Name == CommandReader.HandleMethod &&
            handler.ContainingType.ToDisplayString() == command && HandlerBodies.Of(handler).Any(body => body.Span.Contains(node.Span));
    }

    static bool IsReadOnlyUse(ExpressionSyntax expression, SemanticModel model)
    {
        SyntaxNode subject = expression;
        while (subject.Parent is ParenthesizedExpressionSyntax)
        {
            subject = subject.Parent;
        }

        return subject.Parent switch
        {
            MemberAccessExpressionSyntax member when member.Expression == subject &&
                model.GetSymbolInfo(member).Symbol is IPropertySymbol or IFieldSymbol =>
                !IsWriteTarget(member),
            ConditionalAccessExpressionSyntax access when access.Expression == subject &&
                access.WhenNotNull is MemberBindingExpressionSyntax binding =>
                model.GetSymbolInfo(binding).Symbol is IPropertySymbol or IFieldSymbol && !IsWriteTarget(access),
            IsPatternExpressionSyntax pattern => pattern.Expression == subject,
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.IsExpression) => binary.Left == subject,
            SwitchExpressionSyntax @switch => @switch.GoverningExpression == subject,
            SwitchStatementSyntax @switch => @switch.Expression == subject,
            _ => false
        };
    }

    static bool IsWriteTarget(SyntaxNode node)
    {
        while (node.Parent is ParenthesizedExpressionSyntax)
        {
            node = node.Parent;
        }

        return node.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == node,
            PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
            PostfixUnaryExpressionSyntax postfix => postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression),
            ArgumentSyntax argument => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword),
            RefExpressionSyntax => true,
            _ => false
        };
    }

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

            // A Task or Result carries a value; arbitrary generic consumers (such as projection builders)
            // and delegates naming an event do not. Return signatures remain conservatively broader.
            var valueWrapper = named.Is("System.Threading.Tasks.Task`1") || named.Is("System.Threading.Tasks.ValueTask`1") ||
                named.Is("Cratis.Monads.Result`2");
            var components = named.IsAnonymousType ? named.GetMembers().OfType<IPropertySymbol>().Select(property => property.Type) :
                named.IsTupleType ? named.TupleElements.Select(element => element.Type) :
                named.AllInterfaces.Prepend(named).Where(candidate => candidate.Is("System.Collections.Generic.IEnumerable`1"))
                    .SelectMany(candidate => candidate.TypeArguments).Concat(includeReturnWrappers || valueWrapper ? named.TypeArguments : []);
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
