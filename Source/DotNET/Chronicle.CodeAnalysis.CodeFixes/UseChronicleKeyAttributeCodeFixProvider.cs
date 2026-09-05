// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Code fix that rewrites a command's data annotations <c>[Key]</c> into the Chronicle one.
/// </summary>
/// <remarks>
/// Adding a using for <c>Cratis.Chronicle.Keys</c> would not do: a file marking the data annotations attribute already
/// has a using for its namespace, and with both in scope <c>[Key]</c> is ambiguous (CS0104). The attribute is written
/// out in full and annotated for simplification instead, so it is shortened only where that is unambiguous.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseChronicleKeyAttributeCodeFixProvider)), Shared]
public class UseChronicleKeyAttributeCodeFixProvider : CodeFixProvider
{
    const string Title = "Use the Chronicle Key attribute";
    const string ChronicleKeyAttribute = "Cratis.Chronicle.Keys.Key";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ARCCHR0008_CommandKeyMarkedWithDataAnnotationsKey.Id];

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        var node = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        if (FindKeyAttribute(node) is not { } attribute)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken => RewriteToChronicleKeyAsync(context.Document, attribute, cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    /// <summary>
    /// Finds the attribute to rewrite from the node the diagnostic points at.
    /// </summary>
    /// <param name="node">The node found at the diagnostic's span.</param>
    /// <returns>The attribute, or <see langword="null"/> when the node holds none.</returns>
    /// <remarks>
    /// The node is not always the attribute itself: on a positional record the diagnostic covers the whole
    /// <c>[property: Key]</c> application, target specifier included, and an attribute list can hold more than one
    /// attribute — so the one named Key is picked rather than the first.
    /// </remarks>
    static AttributeSyntax? FindKeyAttribute(SyntaxNode? node) =>
        node switch
        {
            null => null,
            AttributeSyntax attribute => attribute,
            _ => node.FirstAncestorOrSelf<AttributeSyntax>()
                ?? node.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault(IsNamedKey)
        };

    static bool IsNamedKey(AttributeSyntax attribute) =>
        attribute.Name switch
        {
            SimpleNameSyntax simple => IsKeyIdentifier(simple.Identifier.ValueText),
            QualifiedNameSyntax qualified => IsKeyIdentifier(qualified.Right.Identifier.ValueText),
            _ => false
        };

    static bool IsKeyIdentifier(string identifier) =>
        string.Equals(identifier, "Key", StringComparison.Ordinal) ||
        string.Equals(identifier, "KeyAttribute", StringComparison.Ordinal);

    static async Task<Document> RewriteToChronicleKeyAsync(Document document, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var rewritten = attribute
            .WithName(ParseName(ChronicleKeyAttribute).WithAdditionalAnnotations(Simplifier.Annotation))
            .WithTriviaFrom(attribute);

        return document.WithSyntaxRoot(root.ReplaceNode(attribute, rewritten));
    }
}
