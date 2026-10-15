// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports explicit read bypasses and source-visible early aggregate completion in On commands.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProtectedCommandBodyAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [DiagnosticDescriptors.ARCCHR0028_RawReadUnderOn, DiagnosticDescriptors.ARCCHR0035_EarlyProtectedCommit];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var nested = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            var commits = new ConcurrentBag<(INamedTypeSymbol Command, Location Location, bool HasLaterReadOrAppend)>();
            start.RegisterSyntaxNodeAction(node => Analyze(node, nested, commits), SyntaxKind.InvocationExpression);
            start.RegisterCompilationEndAction(end =>
            {
                foreach (var (command, location, hasLaterReadOrAppend) in commits)
                {
                    var reason = nested.ContainsKey(command) ? "in a command invoked as nested" :
                        hasLaterReadOrAppend ? "before a later read or append in the same block" : null;
                    if (reason is not null)
                    {
                        end.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0035_EarlyProtectedCommit, location, command.Name, reason));
                    }
                }
            });
        });
    }

    static void Analyze(
        SyntaxNodeAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, byte> nested,
        ConcurrentBag<(INamedTypeSymbol Command, Location Location, bool HasLaterReadOrAppend)> commits)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var enclosing = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
        if (ReadProtectionSymbols.CommandFor(enclosing) is not { } command || ReadProtectionSymbols.Mode(command) != 2 ||
            context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol called)
        {
            return;
        }
        if (called.Name == "Execute" && ReadProtectionSymbols.Is(called.ContainingType, "Cratis.Arc.Commands.ICommandPipeline", context.Compilation))
        {
            var argument = invocation.ArgumentList.Arguments.FirstOrDefault(candidate => candidate.NameColon?.Name.Identifier.ValueText == "command") ??
                invocation.ArgumentList.Arguments.FirstOrDefault();
            if (argument is not null && context.SemanticModel.GetTypeInfo(argument.Expression, context.CancellationToken).Type is INamedTypeSymbol inner)
            {
                nested.TryAdd(inner, 0);
            }
        }
        if (ReadProtectionSymbols.IsRawRead(called) &&
            (!ReadProtectionSymbols.AdvisoryBody(enclosing) || called.Name == "AllowReads"))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0028_RawReadUnderOn, invocation.GetLocation(), command.Name, called.Name));
        }
        if (called.Name != "Commit" || !ReadProtectionSymbols.Is(called.ContainingType, "Cratis.Arc.Chronicle.Aggregates.AggregateRoot", context.Compilation))
        {
            return;
        }
        commits.Add((command, invocation.GetLocation(), HasLaterReadOrAppend(invocation, context.SemanticModel, ReadProtectionSymbols.AdvisoryBody(enclosing))));
    }

    static bool HasLaterReadOrAppend(InvocationExpressionSyntax commit, SemanticModel model, bool advisoryBody)
    {
        // Only direct sequential statements prove ordering. Never compare source offsets across branches or callbacks.
        var statement = commit.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
        if (statement?.Parent is not BlockSyntax block || statement is ReturnStatementSyntax ||
            commit.Ancestors().TakeWhile(node => node != statement).Any(node => node is AnonymousFunctionExpressionSyntax or ConditionalExpressionSyntax))
        {
            return false;
        }
        return block.Statements.Skip(block.Statements.IndexOf(statement) + 1)
            .TakeWhile(next => next is ExpressionStatementSyntax or LocalDeclarationStatementSyntax or ReturnStatementSyntax)
            .SelectMany(next => next.DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Any(next => !next.Ancestors().TakeWhile(node => node.Parent != block).Any(node => node is AnonymousFunctionExpressionSyntax) &&
                model.GetSymbolInfo(next).Symbol is IMethodSymbol method &&
                ((!advisoryBody && ReadProtectionSymbols.IsRawRead(method)) ||
                 (method.ContainingNamespace.ToDisplayString().StartsWith("Cratis.Chronicle.", StringComparison.Ordinal) &&
                  (method.Name.StartsWith("Append", StringComparison.Ordinal) ||
                   (!advisoryBody && (method.Name == "Get" || method.Name == "GetFor") && ReadProtectionSymbols.Is(method.ContainingType, "Cratis.Chronicle.ReadModels.IDecisionReads", model.Compilation))))));
    }
}
