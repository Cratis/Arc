// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Constraints;

/// <summary>
/// Proves builder identity and unconditional registration before reading a constraint's targets or lifetime.
/// </summary>
internal static class ConstraintBuilderCalls
{
    static readonly HashSet<string> _definitionCalls = new(StringComparer.Ordinal)
    {
        "Unique", "RemovedWith", "ForEventSequences", "ForEventLog", "PerEventSourceType",
        "PerEventStreamType", "PerEventStreamId", "ClosesStreamOn"
    };

    static readonly HashSet<string> _callbackCalls = new(StringComparer.Ordinal)
    {
        "On", "RemovedWith", "WithName", "WithMessage", "WithMode", "IgnoreCasing"
    };

    /// <summary>
    /// Reads only calls inside the callback body, never a uniqueness invocation's receiver chain.
    /// </summary>
    /// <param name="invocation">The uniqueness declaration.</param>
    /// <param name="model">The semantic model owning the declaration.</param>
    /// <returns>The ordered calls, or null if registration cannot be proven.</returns>
    internal static IReadOnlyList<InvocationExpressionSyntax>? Callback(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (InvocationChain.ArgumentOf(invocation) is not LambdaExpressionSyntax callback)
        {
            return null;
        }

        if (ExpressionsIn(callback.Body) is null || HasUnprovenSyntax(callback.Body))
        {
            return null;
        }

        var parameter = ParameterOf(callback, model);
        if (parameter is null)
        {
            return null;
        }

        var calls = callback.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().OrderBy(_ => _.Span.End).ToArray();

        return calls.All(call => IsCallbackCall(call, parameter, model)) ? calls : null;
    }

    /// <summary>
    /// Reads unconditional direct calls on the original builder throughout its defining method.
    /// </summary>
    /// <param name="invocation">The uniqueness declaration.</param>
    /// <param name="model">The semantic model owning the declaration.</param>
    /// <returns>The ordered calls, or null for aliases, escapes or unproven control flow.</returns>
    internal static IReadOnlyList<InvocationExpressionSyntax>? Definition(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var declaration = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var body = (SyntaxNode?)declaration?.Body ?? declaration?.ExpressionBody?.Expression;
        var expressions = body is null ? null : ExpressionsIn(body);
        var builder = Receiver(invocation, model);
        if (expressions is null || builder is null)
        {
            return null;
        }

        if (!IsDefinitionBuilder(builder, declaration!, model))
        {
            return null;
        }

        var calls = expressions.SelectMany(InvocationChain.Sequence).ToArray();

        return calls.All(call => IsDefinitionCall(call, builder, model)) ? calls : null;
    }

    static bool IsDefinitionBuilder(ISymbol builder, MethodDeclarationSyntax declaration, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(declaration) is not IMethodSymbol { Parameters: [var parameter] } method)
        {
            return false;
        }

        var contract = method.ContainingType.FindInterface(WellKnownTypeNames.Constraint);
        var define = contract?.GetMembers(ConstraintReader.DefineMethod).OfType<IMethodSymbol>().FirstOrDefault();
        var implementation = define is null ? null : method.ContainingType.FindImplementationForInterfaceMember(define);

        return SymbolEqualityComparer.Default.Equals(builder, parameter) && SymbolEqualityComparer.Default.Equals(method, implementation);
    }

    static InvocationExpressionSyntax[]? ExpressionsIn(SyntaxNode body) => body switch
    {
        InvocationExpressionSyntax invocation => [invocation],
        BlockSyntax block => ExpressionsIn(block),
        _ => null
    };

    static InvocationExpressionSyntax[]? ExpressionsIn(BlockSyntax block) =>
        block.Statements.All(_ => _ is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax })
            ? block.Statements.Cast<ExpressionStatementSyntax>().Select(_ => (InvocationExpressionSyntax)_.Expression).ToArray()
            : null;

    static bool HasUnprovenSyntax(SyntaxNode body) => body.DescendantNodesAndSelf().Any(node => node switch
    {
        AssignmentExpressionSyntax or ConditionalExpressionSyntax => true,
        LambdaExpressionSyntax lambda => !IsPropertySelector(lambda),
        _ => false
    });

    static bool IsPropertySelector(LambdaExpressionSyntax lambda) =>
        lambda.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } &&
        InvocationChain.NameOf(invocation) == ConstraintReader.On;

    static ISymbol? ParameterOf(LambdaExpressionSyntax callback, SemanticModel model)
    {
        var parameter = callback switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } parenthesized => parenthesized.ParameterList.Parameters[0],
            _ => null
        };

        return parameter is null ? null : model.GetDeclaredSymbol(parameter);
    }

    static ISymbol? Receiver(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var root = InvocationChain.Sequence(invocation)[0];

        return root.Expression is MemberAccessExpressionSyntax member ? model.GetSymbolInfo(member.Expression).Symbol : null;
    }

    static bool IsChronicleCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        method.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Events.Constraints";

    static bool IsCallbackCall(InvocationExpressionSyntax invocation, ISymbol parameter, SemanticModel model)
    {
        if (model.GetConstantValue(invocation).HasValue)
        {
            return true;
        }

        if (!_callbackCalls.Contains(InvocationChain.NameOf(invocation)) || !IsChronicleCall(invocation, model))
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(Receiver(invocation, model), parameter) && SafeArguments(invocation, parameter, model);
    }

    static bool IsDefinitionCall(InvocationExpressionSyntax invocation, ISymbol builder, SemanticModel model)
    {
        if (!_definitionCalls.Contains(InvocationChain.NameOf(invocation)) || !IsChronicleCall(invocation, model))
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(Receiver(invocation, model), builder))
        {
            return false;
        }

        if (InvocationChain.NameOf(invocation) == ConstraintReader.Unique)
        {
            return InvocationChain.TypeArgumentOf(invocation) is null
                ? Callback(invocation, model) is not null
                : invocation.ArgumentList.Arguments.All(_ => model.GetConstantValue(_.Expression).HasValue);
        }

        return SafeArguments(invocation, builder, model);
    }

    static bool SafeArguments(InvocationExpressionSyntax invocation, ISymbol builder, SemanticModel model) =>
        invocation.ArgumentList.Arguments.All(argument => SafeArgument(argument.Expression, builder, model));

    static bool SafeArgument(ExpressionSyntax expression, ISymbol builder, SemanticModel model)
    {
        if (model.GetConstantValue(expression).HasValue)
        {
            return true;
        }

        if (expression.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().Any())
        {
            return false;
        }

        if (expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Any(identifier => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, builder)))
        {
            return false;
        }

        return expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .All(call => model.GetConstantValue(call).HasValue || IsChronicleCall(call, model));
    }
}
