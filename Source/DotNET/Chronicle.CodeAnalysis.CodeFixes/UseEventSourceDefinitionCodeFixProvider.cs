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
/// Replaces matching legacy source/stream attributes with the declared event source definition.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseEventSourceDefinitionCodeFixProvider)), Shared]
public sealed class UseEventSourceDefinitionCodeFixProvider : CodeFixProvider
{
    const string Title = "Use the event source definition";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ["ARCCHR0013"];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        if (!diagnostic.Properties.TryGetValue("Definition", out var definition) || definition is null)
        {
            return;
        }
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var attribute = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AttributeSyntax>();
        var declaration = attribute?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (attribute is null || declaration is null || model is null)
        {
            return;
        }
        var streams = declaration.AttributeLists.SelectMany(list => list.Attributes).Where(candidate =>
            model.GetSymbolInfo(candidate, context.CancellationToken).Symbol is IMethodSymbol constructor &&
            constructor.ContainingType.ToDisplayString() == "Cratis.Chronicle.Events.EventStreamTypeAttribute").ToArray();
        diagnostic.Properties.TryGetValue("Stream", out var stream);

        // An inherited stream or source needs a deliberate declaration change, not a fix on another type's syntax.
        if (stream is not null && streams.Length != 1)
        {
            return;
        }
        var replacement = Attribute(ParseName($"global::Cratis.Arc.Chronicle.Commands.EventSource<{definition}>"));
        if (stream is not null)
        {
            replacement = replacement.WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(stream))))));
        }
        var tracked = declaration.TrackNodes(streams.Append(attribute));
        var updated = tracked.ReplaceNode(tracked.GetCurrentNode(attribute)!, replacement.WithTriviaFrom(attribute));
        foreach (var streamAttribute in streams)
        {
            var current = updated.GetCurrentNode(streamAttribute)!;

            // Keep comments attached to a removed legacy stream attribute on the typed declaration.
            updated = updated.RemoveNode(current.Parent is AttributeListSyntax { Attributes.Count: 1 } list ? list : current, SyntaxRemoveOptions.KeepExteriorTrivia)!;
        }
        var action = CodeAction.Create(
            Title,
            _ => Task.FromResult(context.Document.WithSyntaxRoot(root!.ReplaceNode(declaration, updated))),
            Title);
        context.RegisterCodeFix(action, diagnostic);
    }
}
