// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Constraints;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads Arc's immutable returned-command chains and every refusal branch in declaration order.
/// </summary>
/// <param name="models">The models resolving handler and callback bodies.</param>
/// <param name="constraints">The constraint catalog proving branch batch equivalence.</param>
internal class ReactionInvocations(SemanticModels models, ConstraintCoverage? constraints)
{
    const string InvocationType = "Cratis.Arc.Chronicle.Reactors.CommandInvocation";
    const string InvokeType = "Cratis.Arc.Chronicle.Reactors.Invoke";

    /// <summary>
    /// Reads a whole returned invocation or literal invocation collection.
    /// </summary>
    /// <param name="value">The expression handed to the side effect executor.</param>
    /// <param name="model">The model resolving the returned expression.</param>
    /// <param name="handler">The reactor handler.</param>
    /// <param name="routesAppends">Whether bare branch events inherit unsupported append routing.</param>
    /// <returns>The complete reaction, or null if any invocation or branch cannot be proved.</returns>
    internal ReactionModel? Read(ExpressionSyntax value, SemanticModel model, IMethodSymbol handler, bool routesAppends)
    {
        var items = ReactionReader.ItemsOf(value) ?? [value];
        if (items.Count == 0)
        {
            return null;
        }
        var invocations = items.Select(item => ReadInvocation(item, model, handler, routesAppends)).ToList();

        return invocations.Exists(invocation => invocation is null) ? null :
            new(handler.Parameters[0].Type.Name, [], invocations.OfType<InvocationModel>().ToList());
    }

    static bool IsCommand(IMethodSymbol method, Compilation compilation) => method is { IsStatic: true, Parameters.Length: 1 } &&
        string.Equals(method.Name, "Command", StringComparison.Ordinal) &&
        SymbolEqualityComparer.Default.Equals(method.ContainingType, compilation.GetTypeByMetadataName(InvokeType)) &&
        SymbolEqualityComparer.Default.Equals(method.ReturnType, compilation.GetTypeByMetadataName(InvocationType));

    static ExpressionSyntax? Argument(IInvocationOperation operation, int ordinal) =>
        (operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == ordinal)?.Syntax as ArgumentSyntax)?.Expression;

    static InvocationModel? ReadCommand(ExpressionSyntax expression, SemanticModel model, IMethodSymbol handler)
    {
        if (ReactionEffects.Read(expression, model, handler) is not { } effect || !CommandReader.IsCommand(effect.Type))
        {
            return null;
        }
        var context = handler.Parameters.Length > 1 ? handler.Parameters[1] : null;
        var mappings = ReactionMappings.Read(effect.Creation, model, effect.Type, handler.Parameters[0], context);

        return mappings is null ? null : new(effect.Type.Name, mappings);
    }

    InvocationModel? ReadInvocation(ExpressionSyntax expression, SemanticModel model, IMethodSymbol handler, bool routesAppends)
    {
        while (expression is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses.Expression;
        }
        if (expression is not InvocationExpressionSyntax call || model.GetOperation(call) is not IInvocationOperation operation)
        {
            return null;
        }
        if (IsCommand(operation.TargetMethod, model.Compilation))
        {
            return Argument(operation, 0) is { } command ? ReadCommand(command, model, handler) : null;
        }
        if (!RefusalInvocations.IsBranch(call, model) || call.Expression is not MemberAccessExpressionSyntax member ||
            ReadInvocation(member.Expression, model, handler, routesAppends) is not { } invocation ||
            Argument(operation, 0) is not { } selectorExpression || RefusalSelectors.Read(selectorExpression, model) is not { } selector)
        {
            return null;
        }
        var productions = operation.TargetMethod.Name == "Acknowledge" ? [] :
            Argument(operation, 1) is { } callback ? new RefusalBodies(models, constraints).Read(callback, model, handler, selector.Selector == "constraint", routesAppends) : null;

        return productions is null ? null : invocation with
        {
            OnRefused = [.. invocation.OnRefused, new(selector.Selector, selector.Constraint, productions)]
        };
    }
}
