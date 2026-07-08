// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.CodeAnalysis.CodeFixes;

/// <summary>
/// Code fix that rewrites a [Roles] string literal into a nameof expression against a resolvable enum member.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofForRolesCodeFixProvider)), Shared]
public class UseNameofForRolesCodeFixProvider : CodeFixProvider
{
    const string Title = "Use nameof for role";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ARC0011_RolesArgumentShouldUseNameof.Id];

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not LiteralExpressionSyntax literal || semanticModel is null)
        {
            return;
        }

        var roleName = literal.Token.ValueText;

        if (FindEnumMember(semanticModel.Compilation.GlobalNamespace, roleName) is not { } enumType)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken => RewriteToNameofAsync(context.Document, literal, enumType.Name, roleName, cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Document> RewriteToNameofAsync(Document document, LiteralExpressionSyntax literal, string enumTypeName, string memberName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var nameofExpression = ParseExpression($"nameof({enumTypeName}.{memberName})").WithTriviaFrom(literal);
        return document.WithSyntaxRoot(root.ReplaceNode(literal, nameofExpression));
    }

    static INamedTypeSymbol? FindEnumMember(INamespaceSymbol @namespace, string memberName)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            if (type.TypeKind == TypeKind.Enum && type.GetMembers(memberName).Any(member => member.Kind == SymbolKind.Field))
            {
                return type;
            }
        }

        foreach (var nested in @namespace.GetNamespaceMembers())
        {
            if (FindEnumMember(nested, memberName) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
