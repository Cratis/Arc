// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Offers a local, compiler-checked response identity change when the Guid is intended to identify the event source.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseEventSourceIdForRawGuidResponseCodeFixProvider)), Shared]
public class UseEventSourceIdForRawGuidResponseCodeFixProvider : CodeFixProvider
{
    const string Title = "Use EventSourceId<Guid> for the response identity";
    const string EventSourceIdOfGuid = "global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid>";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ARCCHR0010_RawGuidResponseDoesNotSetEventSourceId.Id];

    /// <inheritdoc/>
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var method = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        var model = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || method?.ReturnType.Span.Contains(diagnostic.Location.SourceSpan) != true ||
            model?.GetDeclaredSymbol(method, cancellationToken) is not IMethodSymbol { IsAbstract: false, IsVirtual: false, IsOverride: false } symbol ||
            symbol.ExplicitInterfaceImplementations.Length > 0 ||
            !symbol.ContainingType.GetAttributes().Any(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, model.Compilation.GetTypeByMetadataName("Cratis.Arc.Commands.ModelBound.CommandAttribute"))))
        {
            return;
        }

        // Never edit an alias declaration or an inherited Handle. Only direct tuple syntax on this command is eligible.
        // Non-async Task.FromResult, Result/OneOf and other invariant factory returns require body rewrites we do not offer.
        var tuple = method.ReturnType as TupleTypeSyntax;
        if (tuple is null && symbol.IsAsync && symbol.ReturnType is INamedTypeSymbol awaitable &&
            (IsType(awaitable, "System.Threading.Tasks.Task`1", model.Compilation) ||
             IsType(awaitable, "System.Threading.Tasks.ValueTask`1", model.Compilation)) &&
            awaitable.TypeArguments[0] is INamedTypeSymbol { IsTupleType: true })
        {
            tuple = method.ReturnType.DescendantNodes().OfType<TypeArgumentListSyntax>()
                .SelectMany(arguments => arguments.Arguments).OfType<TupleTypeSyntax>().FirstOrDefault();
        }

        var guidElements = tuple?.Elements.Where(element =>
            IsType(model.GetTypeInfo(element.Type, cancellationToken).Type, "System.Guid", model.Compilation)).ToArray();
        if (guidElements is not { Length: 1 } || model.Compilation.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        var element = guidElements[0];
        var replacement = element.WithType(ParseTypeName(EventSourceIdOfGuid).WithTriviaFrom(element.Type));
        var changedDocument = context.Document.WithSyntaxRoot(root.ReplaceNode(element, replacement));
        var changedCompilation = await changedDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (changedCompilation?.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) != false)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(Title, _ => Task.FromResult(changedDocument), Title),
            diagnostic);
    }

    static bool IsType(ITypeSymbol? type, string metadataName, Compilation compilation) =>
        type is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, compilation.GetTypeByMetadataName(metadataName));
}
