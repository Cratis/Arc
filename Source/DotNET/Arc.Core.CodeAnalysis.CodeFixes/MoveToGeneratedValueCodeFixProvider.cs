// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.CodeAnalysis.CodeFixes;

/// <summary>
/// Moves a single inline concept generation expression to a generated Handle parameter.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveToGeneratedValueCodeFixProvider)), Shared]
public sealed class MoveToGeneratedValueCodeFixProvider : CodeFixProvider
{
    const string Title = "Move to a [GeneratedValue] parameter";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ["ARC0028"];

    /// <inheritdoc/>
    public override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        if (root?.FindNode(diagnostic.Location.SourceSpan) is not ExpressionSyntax expression || model is null)
        {
            return;
        }

        if (expression.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } method)
        {
            return;
        }

        if (!CanMove(expression, method, model))
        {
            return;
        }

        context.RegisterCodeFix(CodeAction.Create(Title, token => Move(context.Document, expression, method, token), Title), diagnostic);
    }

    static bool CanMove(ExpressionSyntax expression, MethodDeclarationSyntax method, SemanticModel model)
    {
        if (IsNullableConcept(expression, model) || !IsMovableMethod(method, model))
        {
            return false;
        }

        return IsMovableInvocation(expression, model) && !expression.Ancestors().TakeWhile(node => node != method).Any(IsConditionalExecution);
    }

    static bool IsNullableConcept(ExpressionSyntax expression, SemanticModel model)
    {
        var info = model.GetTypeInfo(expression);
        var type = InlineGeneratedValue.IsConcept(info.Type) ? info.Type : info.ConvertedType;

        return type?.NullableAnnotation == NullableAnnotation.Annotated;
    }

    static bool IsMovableMethod(MethodDeclarationSyntax method, SemanticModel model)
    {
        var symbol = model.GetDeclaredSymbol(method)!;
        if (symbol.IsOverride || symbol.IsVirtual)
        {
            return false;
        }

        if (ImplementsInterfaceMethod(symbol))
        {
            return false;
        }

        return !method.ContainsDirectives && InlineGeneratedValue.IsCommandHandle(method, model);
    }

    static bool ImplementsInterfaceMethod(IMethodSymbol method) => method.ContainingType.AllInterfaces
        .SelectMany(contract => contract.GetMembers().OfType<IMethodSymbol>())
        .Any(member => SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(member), method));

    static bool IsMovableInvocation(ExpressionSyntax expression, SemanticModel model)
    {
        var invocations = expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
        if (invocations.Length != 1)
        {
            return false;
        }

        return InlineGeneratedValue.ExpressionFor(invocations[0], model) == expression &&
            invocations[0].ArgumentList.Arguments.All(argument => model.GetConstantValue(argument.Expression).HasValue);
    }

    static bool IsConditionalExecution(SyntaxNode node) =>
        (node is (StatementSyntax and not (BlockSyntax or ReturnStatementSyntax or LocalDeclarationStatementSyntax or ExpressionStatementSyntax)) or
            ConditionalExpressionSyntax or BinaryExpressionSyntax or SwitchExpressionSyntax or ConditionalAccessExpressionSyntax or QueryExpressionSyntax) || InlineGeneratedValue.IsNestedFunction(node);

    static async Task<Document> Move(Document document, ExpressionSyntax expression, MethodDeclarationSyntax method, CancellationToken token)
    {
        var root = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false))!;
        var model = (await document.GetSemanticModelAsync(token).ConfigureAwait(false))!;
        var info = model.GetTypeInfo(expression, token);
        var type = InlineGeneratedValue.IsConcept(info.Type) ? info.Type! : info.ConvertedType!;
        var name = UniqueName(expression, method, type.Name, model);
        var identifier = SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : $"@{name}";
        var parameter = ParameterFor(type, identifier);
        var index = method.ParameterList.Parameters.TakeWhile(candidate => candidate.Default is null && !candidate.Modifiers.Any(SyntaxKind.ParamsKeyword)).Count();
        var replacement = method.ReplaceNode(expression, IdentifierName(identifier).WithTriviaFrom(expression))
            .WithParameterList(method.ParameterList.WithParameters(method.ParameterList.Parameters.Insert(index, parameter)))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var changedDocument = document.WithSyntaxRoot(root.ReplaceNode(method, replacement));
        changedDocument = await ImportAdder.AddImportsAsync(changedDocument, Simplifier.Annotation, cancellationToken: token).ConfigureAwait(false);

        return await Simplifier.ReduceAsync(changedDocument, Simplifier.Annotation, cancellationToken: token).ConfigureAwait(false);
    }

    static ParameterSyntax ParameterFor(ITypeSymbol type, string identifier)
    {
        var attributeName = ParseName("global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute").WithAdditionalAnnotations(Simplifier.Annotation);
        var attributes = AttributeList(SingletonSeparatedList(Attribute(attributeName)));

        return Parameter(Identifier(identifier))
            .WithType(ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).WithAdditionalAnnotations(Simplifier.Annotation))
            .WithAttributeLists(SingletonList(attributes));
    }

    static string UniqueName(ExpressionSyntax expression, MethodDeclarationSyntax method, string typeName, SemanticModel model)
    {
        var preferred = expression.Parent switch
        {
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variable } => variable.Identifier.ValueText,
            ArgumentSyntax argument => ArgumentName(argument, model) ?? CamelCase(typeName),
            _ => CamelCase(typeName)
        };
        var identifiers = method.DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText);
        var names = new HashSet<string>(identifiers.Concat(model.LookupSymbols(expression.SpanStart).Select(symbol => symbol.Name)), StringComparer.Ordinal);
        var symbol = model.GetDeclaredSymbol(method)!;
        names.UnionWith(symbol.Parameters
            .SelectMany(parameter => parameter.GetAttributes())
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == "Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute")
            .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value)
            .OfType<string>());

        var candidate = preferred;
        for (var suffix = 2; names.Contains(candidate); suffix++)
        {
            candidate = $"{preferred}{suffix}";
        }

        return candidate;
    }

    static string? ArgumentName(ArgumentSyntax argument, SemanticModel model) =>
        model.GetOperation(argument) is Microsoft.CodeAnalysis.Operations.IArgumentOperation operation ? operation.Parameter?.Name : null;

    static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);
}
