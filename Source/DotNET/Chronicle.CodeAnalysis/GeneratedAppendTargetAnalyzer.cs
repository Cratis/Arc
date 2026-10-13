// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Warns about command-key aggregates accompanying returned events for a generated source.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedAppendTargetAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0022_AggregateOnGeneratedTarget];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
    }

    static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol { Name: "Handle", IsStatic: false } method ||
            !IsKnownCommand(method.ContainingType))
        {
            return;
        }

        var aggregates = method.Parameters.Where(IsInjectedAggregate).ToArray();
        if (aggregates.Length == 0)
        {
            return;
        }

        var targets = syntax.DescendantNodes(node => !IsNestedFunction(node)).OfType<ExpressionSyntax>()
            .Select(expression => TargetOf(expression, syntax, context.SemanticModel)).OfType<IParameterSymbol>().Distinct(SymbolEqualityComparer.Default);
        foreach (var target in targets)
        {
            foreach (var aggregate in aggregates)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0022_AggregateOnGeneratedTarget, aggregate.Locations[0], aggregate.Name, target!.Name));
            }
        }
    }

    static bool IsKnownCommand(INamedTypeSymbol type)
    {
        var hierarchy = Hierarchy(type).ToArray();

        // Provide can supply an aggregate for a different source; custom id providers are not statically known.
        return hierarchy.Any(candidate => HasAttribute(candidate, "Cratis.Arc.Commands.ModelBound.CommandAttribute")) &&
            !hierarchy.Any(candidate => candidate.GetMembers("Provide").OfType<IMethodSymbol>().Any()) &&
            !type.AllInterfaces.Any(contract => contract.ToDisplayString() == "Cratis.Chronicle.Events.ICanProvideEventSourceId") &&
            hierarchy.SelectMany(candidate => candidate.GetMembers().OfType<IPropertySymbol>()).Any(property => IsEventSourceId(property.Type));
    }

    static bool IsInjectedAggregate(IParameterSymbol parameter) => parameter.Type.TypeKind == TypeKind.Class &&
        parameter.Type.AllInterfaces.Any(contract => contract.ToDisplayString() == "Cratis.Arc.Chronicle.Aggregates.IAggregateRoot") &&
        !HasAttribute(parameter, "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute") &&
        !HasAttribute(parameter, "Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute");

    static IParameterSymbol? TargetOf(ExpressionSyntax expression, MethodDeclarationSyntax method, SemanticModel model) => expression switch
    {
        ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax => IsReturned(expression, method) ? GeneratedTarget(expression, model) : null,
        InvocationExpressionSyntax invocation => AppendTarget(invocation, model),
        _ => null
    };

    static IParameterSymbol? AppendTarget(InvocationExpressionSyntax expression, SemanticModel model)
    {
        if (model.GetOperation(expression) is not Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation)
        {
            return null;
        }

        if (!IsSequenceAppend(invocation.TargetMethod))
        {
            return null;
        }

        return ParameterTarget(invocation.Arguments, model);
    }

    static bool IsSequenceAppend(IMethodSymbol method) => method.Name.StartsWith("Append", StringComparison.Ordinal) &&
        method.ContainingType.ToDisplayString() == "Cratis.Chronicle.EventSequences.IEventSequence";

    static IParameterSymbol? GeneratedTarget(ExpressionSyntax expression, SemanticModel model)
    {
        if (model.GetTypeInfo(expression).Type?.ToDisplayString() != "Cratis.Chronicle.EventSequences.EventForEventSourceId" ||
            model.GetOperation(expression) is not Microsoft.CodeAnalysis.Operations.IObjectCreationOperation creation)
        {
            return null;
        }

        if (creation.Initializer?.Initializers.OfType<Microsoft.CodeAnalysis.Operations.ISimpleAssignmentOperation>()
            .Any(assignment => assignment.Target is Microsoft.CodeAnalysis.Operations.IPropertyReferenceOperation property && property.Property.Name == "EventSourceId") == true)
        {
            return null;
        }

        return ParameterTarget(creation.Arguments, model);
    }

    static IParameterSymbol? ParameterTarget(ImmutableArray<Microsoft.CodeAnalysis.Operations.IArgumentOperation> arguments, SemanticModel model)
    {
        var argument = arguments.FirstOrDefault(candidate => string.Equals(candidate.Parameter?.Name, "eventSourceId", StringComparison.OrdinalIgnoreCase));
        if (argument?.Syntax is not ArgumentSyntax syntax || model.GetSymbolInfo(syntax.Expression).Symbol is not IParameterSymbol parameter)
        {
            return null;
        }

        return HasAttribute(parameter, "Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute") ? parameter : null;
    }

    static bool IsReturned(ExpressionSyntax expression, MethodDeclarationSyntax method)
    {
        var outer = expression.Parent is ExpressionElementSyntax { Parent: CollectionExpressionSyntax collection } ? collection : expression;

        return outer.Parent is ReturnStatementSyntax || (method.ExpressionBody is not null && method.ExpressionBody.Expression == outer);
    }

    static bool IsEventSourceId(ITypeSymbol type) => Hierarchy(type).Any(candidate => candidate.ToDisplayString() == "Cratis.Chronicle.Events.EventSourceId" ||
        candidate.OriginalDefinition.ToDisplayString() == "Cratis.Chronicle.Events.EventSourceId<T>");

    static bool IsNestedFunction(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

    static bool HasAttribute(ISymbol symbol, string name) => symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == name);

    static IEnumerable<ITypeSymbol> Hierarchy(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }
}
