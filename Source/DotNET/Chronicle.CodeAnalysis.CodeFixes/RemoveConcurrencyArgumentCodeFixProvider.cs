// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Removes the <c>concurrency: true</c> argument that exact concurrency scopes make redundant.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveConcurrencyArgumentCodeFixProvider)), Shared]
public class RemoveConcurrencyArgumentCodeFixProvider : CodeFixProvider
{
    const string Title = "Remove concurrency: true";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ARCCHR0016_ConcurrencyAttributeIgnoredByExactScopes.Id];

    /// <inheritdoc/>
    public sealed override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var argument = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<AttributeArgumentSyntax>();
        if (root is null || argument?.Parent is not AttributeArgumentListSyntax list || list.Parent is not AttributeSyntax attribute)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                _ =>
                {
                    var remaining = list.Arguments.Remove(argument);
                    var changed = remaining.Count == 0
                        ? attribute.WithArgumentList(null)
                        : attribute.WithArgumentList(list.WithArguments(remaining));
                    return Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(attribute, changed)));
                },
                Title),
            diagnostic);
    }
}
