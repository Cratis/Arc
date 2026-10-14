// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Replaces a misspelled lookup string with nameof for the suggested command property.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofForLookUpByCodeFixProvider)), Shared]
public class UseNameofForLookUpByCodeFixProvider : CodeFixProvider
{
    const string Title = "Use nameof for the suggested lookup property";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ["ARCCHR0031"];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        if (!diagnostic.Properties.TryGetValue("SuggestedProperty", out var property) || property is null)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var parameter = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ParameterSyntax>();
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var argument = parameter?.AttributeLists.SelectMany(_ => _.Attributes)
            .Where(attribute => model?.GetSymbolInfo(attribute, context.CancellationToken).Symbol is IMethodSymbol constructor &&
                constructor.ContainingType.ToDisplayString() == "Cratis.Arc.Commands.LookUpByAttribute")
            .SelectMany(attribute => attribute.ArgumentList?.Arguments ?? default)
            .FirstOrDefault(_ => _.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression));
        if (argument is null || model?.GetDeclaredSymbol(parameter!, context.CancellationToken) is not IParameterSymbol symbol)
        {
            return;
        }

        var command = symbol.ContainingSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var identifier = SyntaxFacts.GetKeywordKind(property) != SyntaxKind.None ? $"@{property}" : property;
        var expression = ParseExpression($"nameof({command}.{identifier})").WithTriviaFrom(argument.Expression);
        var action = CodeAction.Create(
            Title,
            _ => Task.FromResult(context.Document.WithSyntaxRoot(root!.ReplaceNode(argument.Expression, expression))),
            Title);
        context.RegisterCodeFix(action, diagnostic);
    }
}
