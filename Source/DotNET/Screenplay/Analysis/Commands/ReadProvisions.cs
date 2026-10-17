// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads a Provide method that only rejects on a read and passes the read on.
/// </summary>
/// <param name="models">The source models.</param>
/// <param name="conditions">The guard comparison reader.</param>
/// <param name="diagnostics">The diagnostic sink.</param>
internal sealed class ReadProvisions(SemanticModels models, ReadConditions conditions, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads one Provide method.
    /// </summary>
    /// <param name="provide">The Provide method.</param>
    /// <param name="scope">The command being read.</param>
    /// <returns>What the method rejects and passes on, or null when it provisions anything else.</returns>
    internal ProvisionRead? Read(IMethodSymbol provide, ReadScope scope)
    {
        if (HandlerBodies.Of(provide).ToArray() is not [var body] || models.For(body.SyntaxTree) is not { } model)
        {
            Report("Provide() lives in code whose body is unavailable", scope);

            return null;
        }

        var read = body switch
        {
            ConditionalExpressionSyntax conditional => Conditional(conditional, model, scope),
            BlockSyntax block => Block(block, model, scope),
            _ => null
        };
        if (read is null)
        {
            Report("Provide() contains provisioning behavior that lives in code; only proven reads are described", scope);
        }

        return read;
    }

    ProvisionRead? Conditional(ConditionalExpressionSyntax conditional, SemanticModel model, ReadScope scope)
    {
        var errorOnTrue = ProtectedReadGuards.ErrorMessage(conditional.WhenTrue, model);
        var errorOnFalse = ProtectedReadGuards.ErrorMessage(conditional.WhenFalse, model);
        var success = errorOnTrue is not null ? conditional.WhenFalse : conditional.WhenTrue;

        return (errorOnTrue ?? errorOnFalse) is { } message && scope.Sources.ReadPath(success, model) is not null &&
            conditions.Condition(conditional.Condition, model, scope, errorOnTrue is not null) is { } condition
            ? new([new(condition, message)], success, model)
            : null;
    }

    ProvisionRead? Block(BlockSyntax block, SemanticModel model, ReadScope scope)
    {
        if (block.Statements.LastOrDefault() is not ReturnStatementSyntax { Expression: { } success } || scope.Sources.ReadPath(success, model) is null)
        {
            return null;
        }

        var requirements = new List<CommandRequirementModel>();
        foreach (var statement in block.Statements.Take(block.Statements.Count - 1).Where(statement => !scope.Sources.ConsumedGuards.Contains(statement)))
        {
            if (Requirement(statement, model, scope) is not { } requirement)
            {
                return null;
            }

            requirements.Add(requirement);
        }

        return new(requirements, success, model);
    }

    CommandRequirementModel? Requirement(StatementSyntax statement, SemanticModel model, ReadScope scope)
    {
        var returned = statement is IfStatementSyntax { Statement: BlockSyntax { Statements: [ReturnStatementSyntax error] } } ? error : (statement as IfStatementSyntax)?.Statement as ReturnStatementSyntax;

        return statement is IfStatementSyntax { Else: null } guard && returned?.Expression is { } expression && ProtectedReadGuards.ErrorMessage(expression, model) is { } message &&
            conditions.Condition(guard.Condition, model, scope, true) is { } condition
            ? new(condition, message)
            : null;
    }

    void Report(string message, ReadScope scope) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandProvisioning, message, scope.Location);
}
