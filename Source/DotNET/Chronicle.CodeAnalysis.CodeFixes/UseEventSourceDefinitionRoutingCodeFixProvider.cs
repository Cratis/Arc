// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Replaces legacy wrapper route strings with their matching event source definition and declared stream.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseEventSourceDefinitionRoutingCodeFixProvider)), Shared]
public sealed class UseEventSourceDefinitionRoutingCodeFixProvider : CodeFixProvider
{
    const string Title = "Use event source definition routing";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.ARCCHR0021_UseEventSourceDefinitionRouting.Id];

    /// <inheritdoc/>
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var initializer = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InitializerExpressionSyntax>();
        if (root is null || initializer is null ||
            !diagnostic.Properties.TryGetValue("Definition", out var definition) ||
            !diagnostic.Properties.TryGetValue("Stream", out var stream))
        {
            return;
        }

        var replacement = Rewrite(initializer, definition!, stream!).WithAdditionalAnnotations(Formatter.Annotation);
        context.RegisterCodeFix(CodeAction.Create(Title, _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(initializer, replacement))), Title), diagnostic);
    }

    static InitializerExpressionSyntax Rewrite(InitializerExpressionSyntax initializer, string definition, string stream)
    {
        var sourceAssignment = Assignment("EventSource", SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(definition)));
        var streamAssignment = Assignment("EventStream", SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(stream)));
        var original = initializer.Expressions;
        var ignoredStream = original.FirstOrDefault(expression => IsAssignment(expression, "EventStream"));
        if (ignoredStream is not null && original.Any(expression => IsAssignment(expression, "EventStreamType")))
        {
            original = original.Remove(ignoredStream);
        }

        var changed = initializer.WithExpressions(original);
        var expressions = changed.ReplaceNodes(changed.Expressions, (expression, _) => ReplaceRouting(expression, sourceAssignment, streamAssignment)).Expressions;
        if (!expressions.Any(expression => IsAssignment(expression, "EventSource")))
        {
            expressions = expressions.Add(sourceAssignment);
        }

        if (!expressions.Any(expression => IsAssignment(expression, "EventStream")))
        {
            expressions = expressions.Add(streamAssignment);
        }

        return initializer.WithExpressions(expressions);
    }

    static ExpressionSyntax ReplaceRouting(ExpressionSyntax expression, ExpressionSyntax source, ExpressionSyntax stream) => expression switch
    {
        AssignmentExpressionSyntax { Left: IdentifierNameSyntax name } when name.Identifier.ValueText == "EventSourceType" => source.WithTriviaFrom(expression),
        AssignmentExpressionSyntax { Left: IdentifierNameSyntax name } when name.Identifier.ValueText == "EventStreamType" || name.Identifier.ValueText == "EventStream" => stream.WithTriviaFrom(expression),
        _ => expression
    };

    static bool IsAssignment(ExpressionSyntax expression, string name) => expression is AssignmentExpressionSyntax { Left: IdentifierNameSyntax left } && left.Identifier.ValueText == name;

    static AssignmentExpressionSyntax Assignment(string name, ExpressionSyntax value) =>
        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(name), value);
}
