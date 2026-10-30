// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Proves the renderer's single enum-event scenario without aggregating independent witnesses.
/// </summary>
/// <param name="name">The recovered authored name.</param>
/// <param name="redundant">The assertion implied by the complete assertion.</param>
/// <param name="command">The exactly executed command.</param>
/// <param name="fixture">The concrete fixture owning the rendered fields.</param>
internal sealed class RenderedCommandSpecification(string name, InvocationExpressionSyntax redundant, INamedTypeSymbol command, INamedTypeSymbol fixture)
{
    /// <summary>Gets the authored name.</summary>
    internal string Name { get; } = name;

    /// <summary>
    /// Describes only the generated shape whose complete assertion implies its existence assertion.
    /// </summary>
    /// <param name="type">The concrete scenario type.</param>
    /// <param name="models">The analyzed source models.</param>
    /// <returns>The exact rendered shape, or null.</returns>
    internal static RenderedCommandSpecification? Describe(INamedTypeSymbol type, SemanticModels models)
    {
        if (GeneratedName(type) is not { } generatedName ||
            !type.BaseType.Is(WellKnownTypeNames.Specification) ||
            type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).ToList() is not [TypeDeclarationSyntax declaration] ||
            models.For(declaration.SyntaxTree) is not { } model ||
            ScenarioField(type, model.Compilation) is not { Type: INamedTypeSymbol { TypeArguments: [INamedTypeSymbol command] } } scenario ||
            !CommandReader.IsCommand(command) ||
            !HasOnlyRenderedCalls(declaration, model) ||
            !ExecutesOnce(type, scenario, command, model))
        {
            return null;
        }

        var assertions = SpecificationMembers.AssertionsIn(type).SelectMany(HandlerBodies.Of)
            .SelectMany(body => body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .Where(call => model.GetSymbolInfo(call).Symbol is IMethodSymbol method && SpecificationAssertions.AppendedEventOf(method) is not null)
                .Select(call => (Call: call, Body: body))).ToList();
        if (assertions is not [var first, var second] ||
            !StepsTaken.AlwaysCompletesThrough(first.Call, first.Body) || !StepsTaken.AlwaysCompletesThrough(second.Call, second.Body) ||
            !SameAssertion(first.Call, second.Call, scenario, command, model))
        {
            return null;
        }

        var firstPredicate = Predicate(first.Call, model);
        var secondPredicate = Predicate(second.Call, model);
        var redundant = IsTrue(firstPredicate) ? first.Call : IsTrue(secondPredicate) ? second.Call : null;
        var complete = IsTrue(firstPredicate) ? secondPredicate : firstPredicate;
        return redundant is not null && IsDirectEnumEquality(complete, type, model)
            ? new(generatedName, redundant, command, type)
            : null;
    }

    /// <summary>Reads a rendered command argument through the normal typed literal reader.</summary>
    /// <param name="expression">The original use.</param>
    /// <param name="expected">The exact formal type.</param>
    /// <param name="sources">The normal typed literal reader.</param>
    /// <param name="model">The use's semantic model.</param>
    /// <returns>The proven literal, or null.</returns>
    internal LiteralSource? ReadLiteral(ExpressionSyntax expression, ITypeSymbol expected, MappingSourceReader sources, SemanticModel model)
    {
        var value = EnumInitializer(expression, expected, model) ?? UuidConstruction(expression, model, expected);
        return value is null ? null : sources.ReadQueryLiteral(value, model, expected, expected);
    }

    /// <summary>Finds the direct UUID concept construction emitted by the renderer.</summary>
    /// <param name="expression">The command argument.</param>
    /// <param name="model">The owning semantic model.</param>
    /// <param name="expected">The exact formal type.</param>
    /// <returns>The direct construction, or null.</returns>
    internal ExpressionSyntax? UuidConstruction(ExpressionSyntax expression, SemanticModel model, ITypeSymbol expected)
    {
        expression = MappingSourceReader.Unwrap(expression);
        if (expression is IdentifierNameSyntax && model.GetSymbolInfo(expression).Symbol is IFieldSymbol { IsStatic: true } field &&
            RenderedFieldInitializer.Read(field, fixture, model.Compilation) is { } initializer && initializer.SyntaxTree == expression.SyntaxTree)
        {
            expression = initializer;
        }

        return expression is ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument], Initializer: null } &&
            SymbolEqualityComparer.IncludeNullability.Equals(model.GetTypeInfo(expression).Type, expected) &&
            expected.FindBase(WellKnownTypeNames.ConceptAs) is { TypeArguments: [var backing] } && backing.Is("System.Guid") &&
            argument.Expression is InvocationExpressionSyntax parse &&
            model.GetSymbolInfo(parse).Symbol is IMethodSymbol { ContainingType: var owner } method &&
            string.Equals(method.Name, "Parse", StringComparison.Ordinal) && owner.Is("System.Guid")
                ? expression
                : null;
    }

    /// <summary>Reads the descriptor-owned single-hop enum value.</summary>
    /// <param name="expression">The original field use.</param>
    /// <param name="expected">The exact expected enum.</param>
    /// <param name="model">The use's model.</param>
    /// <returns>The direct enum member initializer, or null.</returns>
    internal ExpressionSyntax? EnumInitializer(ExpressionSyntax expression, ITypeSymbol expected, SemanticModel model) =>
        RenderedEnumInitializer.Read(expression, expected, fixture, model);

    /// <summary>Determines whether this is the one redundant existence assertion.</summary>
    /// <param name="invocation">The assertion to check.</param>
    /// <returns>Whether the complete assertion implies this exact call.</returns>
    internal bool IsRedundant(InvocationExpressionSyntax invocation) =>
        invocation.SyntaxTree == redundant.SyntaxTree && invocation.Span == redundant.Span;

    /// <summary>Determines whether a value belongs to the exactly executed command.</summary>
    /// <param name="type">The artifact receiving the value.</param>
    /// <returns>Whether the artifact is the command.</returns>
    internal bool IsCommand(ITypeSymbol type) => SymbolEqualityComparer.Default.Equals(command, type);

    static string? GeneratedName(INamedTypeSymbol type)
    {
        var words = type.Name.Split('_');
        if (type.ContainingType is not null || type.ContainingNamespace.Name != type.Name ||
            words.Length < 2 || words[0] != "when" ||
            words.Skip(1).Any(word => word.Length == 0 || word.Any(character => character is < 'a' or > 'z')))
        {
            return null;
        }

        var name = new ScreenplayNaming().ToDeclarationName(string.Join('_', words.Skip(1)));
        var slice = type.ContainingNamespace.ContainingNamespace;
        return TypesBelow(slice).Any(candidate => !SymbolEqualityComparer.Default.Equals(candidate, type) &&
            new ScreenplayNaming().ToDeclarationName(SpecificationPlacement.NameOf(candidate, slice.ToDisplayString())) == name)
                ? null
                : name;
    }

    static IEnumerable<INamedTypeSymbol> TypesBelow(INamespaceSymbol space) =>
        space.GetTypeMembers().Concat(space.GetNamespaceMembers().SelectMany(TypesBelow));

    static IFieldSymbol? ScenarioField(INamedTypeSymbol type, Compilation compilation) =>
        type.GetMembers().OfType<IFieldSymbol>().Where(field =>
            field.Type is INamedTypeSymbol named && named.OriginalDefinition.Is(WellKnownTypeNames.CommandScenario)).ToList() is [var scenario] &&
        !scenario.IsStatic && RenderedFieldInitializer.Read(scenario, type, compilation) is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null }
            ? scenario
            : null;

    static bool HasOnlyRenderedCalls(TypeDeclarationSyntax declaration, SemanticModel model)
    {
        var methods = declaration.Members.OfType<BaseMethodDeclarationSyntax>().ToList();
        if (methods.SelectMany(method => method.DescendantNodes()).Any(node => node is AnonymousFunctionExpressionSyntax { Parent: not ArgumentSyntax } or LocalFunctionStatementSyntax))
        {
            return false;
        }

        var calls = methods.SelectMany(method => method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Select(call => model.GetSymbolInfo(call).Symbol as IMethodSymbol).ToList();
        return calls.Count(method => method is not null && SpecificationCalls.IsExecution(method)) == 1 &&
            calls.TrueForAll(method => method is not null &&
                (SpecificationAssertions.AppendedEventOf(method) is not null ||
                 SpecificationCalls.IsExecution(method) ||
                 (method.Name == "Dispose" && (method.ContainingType.OriginalDefinition.Is(WellKnownTypeNames.CommandScenario) ||
                    method.ContainingType.Is("Cratis.Arc.Chronicle.Testing.Commands.CommandScenarioChronicleAssertionExtensions"))) ||
                 (method.Name == "ShouldBeSuccessful" && (method.ContainingType.Is("Cratis.Arc.Testing.Commands.CommandResultShouldExtensions") ||
                    method.ContainingType.Is("Cratis.Arc.Chronicle.Testing.Commands.CommandResultExtensions")))));
    }

    static bool ExecutesOnce(INamedTypeSymbol type, IFieldSymbol scenario, INamedTypeSymbol command, SemanticModel model)
    {
        var bodies = SpecificationMembers.MethodsIn(type, SpecificationMembers.BecauseMethod).SelectMany(HandlerBodies.Of).ToList();
        if (bodies is not [var body] ||
            body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToList() is not [var execution] ||
            !StepsTaken.AlwaysCompletesThrough(execution, body) ||
            model.GetSymbolInfo(execution).Symbol is not IMethodSymbol { Name: "Execute" } method ||
            !SpecificationCalls.IsExecution(method) || !OnScenario(execution, scenario, model) ||
            execution.ArgumentList.Arguments is not [var argument])
        {
            return false;
        }

        return argument.Expression is BaseObjectCreationExpressionSyntax &&
            SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(argument.Expression).Type, command);
    }

    static bool SameAssertion(InvocationExpressionSyntax first, InvocationExpressionSyntax second, IFieldSymbol scenario, INamedTypeSymbol command, SemanticModel model)
    {
        if (model.GetSymbolInfo(first).Symbol is not IMethodSymbol firstMethod ||
            model.GetSymbolInfo(second).Symbol is not IMethodSymbol secondMethod ||
            firstMethod.TypeArguments is not [var firstCommand, var firstEvent] ||
            secondMethod.TypeArguments is not [var secondCommand, var secondEvent] ||
            !SymbolEqualityComparer.Default.Equals(firstCommand, command) ||
            !SymbolEqualityComparer.Default.Equals(secondCommand, command) ||
            !SymbolEqualityComparer.Default.Equals(firstEvent, secondEvent) ||
            firstEvent.DeclaredProperties().ToList() is not [{ Type.TypeKind: TypeKind.Enum }] ||
            !OnScenario(first, scenario, model) || !OnScenario(second, scenario, model))
        {
            return false;
        }

        var identifier = new CommandIdentifierReader(new([model.Compilation]), new ScreenplayDiagnostics()).Read(command, command.Name);
        if (command.DeclaredProperties().SingleOrDefault(property => property.Name == identifier) is not { Type: var expectedSourceType })
        {
            return false;
        }

        var firstSource = ConcreteSource(first, firstMethod, scenario.ContainingType, expectedSourceType, model);
        var secondSource = ConcreteSource(second, secondMethod, scenario.ContainingType, expectedSourceType, model);
        return firstSource is not null && Equals(firstSource, secondSource);
    }

    static LiteralSource? ConcreteSource(InvocationExpressionSyntax call, IMethodSymbol method, INamedTypeSymbol fixture, ITypeSymbol expected, SemanticModel model)
    {
        var source = DotNetInvocations.ArgumentForParameter(call, method, "eventSourceId", model)?.Expression;
        if (source is IdentifierNameSyntax && model.GetSymbolInfo(source).Symbol is IFieldSymbol { IsStatic: true } field &&
            RenderedFieldInitializer.Read(field, fixture, model.Compilation) is { } initializer)
        {
            source = initializer;
        }

        return source is not null && source.SyntaxTree == call.SyntaxTree
            ? new MappingSourceReader(new ScreenplayDiagnostics()).ReadQueryLiteral(source, model, expected, expected)
            : null;
    }

    static bool OnScenario(InvocationExpressionSyntax call, IFieldSymbol scenario, SemanticModel model) =>
        call.Expression is MemberAccessExpressionSyntax { Expression: var receiver } &&
        (receiver is IdentifierNameSyntax || receiver is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }) &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, scenario);

    static LambdaExpressionSyntax? Predicate(InvocationExpressionSyntax call, SemanticModel model) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol method
            ? DotNetInvocations.ArgumentForParameter(call, method, "predicate", model)?.Expression as LambdaExpressionSyntax
            : null;

    static bool IsTrue(LambdaExpressionSyntax? predicate) => predicate?.Body.IsKind(SyntaxKind.TrueLiteralExpression) == true;

    static bool IsDirectEnumEquality(LambdaExpressionSyntax? predicate, INamedTypeSymbol fixture, SemanticModel model)
    {
        if (predicate is not SimpleLambdaExpressionSyntax { Body: BinaryExpressionSyntax equality } simple ||
            !equality.IsKind(SyntaxKind.EqualsExpression) ||
            equality.Left is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } property ||
            model.GetSymbolInfo(property).Symbol is not IPropertySymbol { Type.TypeKind: TypeKind.Enum } member ||
            model.GetDeclaredSymbol(simple.Parameter) is not { } parameter)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, parameter) &&
            SymbolEqualityComparer.Default.Equals(member.ContainingType, parameter.Type) &&
            (equality.Right is IdentifierNameSyntax or MemberAccessExpressionSyntax) &&
            SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(equality.Right).Type, member.Type) &&
            (model.GetConstantValue(equality.Right).HasValue || RenderedEnumInitializer.Read(equality.Right, member.Type, fixture, model) is not null);
    }
}
