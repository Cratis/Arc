// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that flags a command Handle() method returning a Task without awaiting anything.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandHandleTaskWrappingAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string HandleMethodName = "Handle";
    const string TaskTypeName = "System.Threading.Tasks.Task";
    const string TaskOfTTypeName = "System.Threading.Tasks.Task`1";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARC0010_CommandHandleWrapsSynchronousResultInTask];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        if (method.Identifier.ValueText != HandleMethodName)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(method) is not IMethodSymbol methodSymbol ||
            methodSymbol.IsStatic ||
            methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        if (!HasCommandAttribute(methodSymbol.ContainingType) || !ReturnsTask(methodSymbol.ReturnType, context.Compilation))
        {
            return;
        }

        var isAsync = methodSymbol.IsAsync;

        // An async Handle() with no await is noise. A non-async Handle() that only produces a synchronous
        // Task wrapper (Task.FromResult / Task.CompletedTask) is the same noise CS1998 cannot see.
        var shouldReport = isAsync
            ? !ContainsOwnAwait(method)
            : ProducesOnlySynchronousTaskWrapper(method, context.SemanticModel);

        if (!shouldReport)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARC0010_CommandHandleWrapsSynchronousResultInTask,
            method.Identifier.GetLocation(),
            methodSymbol.ContainingType.Name));
    }

    static bool HasCommandAttribute(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == CommandAttributeName);

    static bool ReturnsTask(ITypeSymbol returnType, Compilation compilation)
    {
        if (returnType is not INamedTypeSymbol namedReturnType)
        {
            return false;
        }

        var taskType = compilation.GetTypeByMetadataName(TaskTypeName);
        var taskOfTType = compilation.GetTypeByMetadataName(TaskOfTTypeName);

        return SymbolEqualityComparer.Default.Equals(namedReturnType, taskType) ||
               SymbolEqualityComparer.Default.Equals(namedReturnType.OriginalDefinition, taskOfTType);
    }

    static bool ContainsOwnAwait(MethodDeclarationSyntax method)
    {
        var body = (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression;

        if (body is null)
        {
            return false;
        }

        return body.DescendantNodesAndSelf(node => node == body || !IsNestedFunction(node))
            .OfType<AwaitExpressionSyntax>()
            .Any();
    }

    static bool ProducesOnlySynchronousTaskWrapper(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        var producedExpressions = GetProducedTaskExpressions(method).ToArray();

        return producedExpressions.Length > 0 &&
            producedExpressions.All(expression => IsSynchronousTaskWrapper(expression, semanticModel));
    }

    static IEnumerable<ExpressionSyntax> GetProducedTaskExpressions(MethodDeclarationSyntax method)
    {
        if (method.ExpressionBody is not null)
        {
            yield return method.ExpressionBody.Expression;
            yield break;
        }

        if (method.Body is null)
        {
            yield break;
        }

        var returnExpressions = method.Body
            .DescendantNodes(node => node == method.Body || !IsNestedFunction(node))
            .OfType<ReturnStatementSyntax>()
            .Select(statement => statement.Expression)
            .Where(expression => expression is not null);

        foreach (var expression in returnExpressions)
        {
            yield return expression!;
        }
    }

    static bool IsSynchronousTaskWrapper(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(expression is InvocationExpressionSyntax invocation ? invocation.Expression : expression).Symbol;

        return symbol switch
        {
            IMethodSymbol method => method.Name == "FromResult" && IsTaskType(method.ContainingType),
            IPropertySymbol property => property.Name == "CompletedTask" && IsTaskType(property.ContainingType),
            _ => false
        };
    }

    static bool IsTaskType(INamedTypeSymbol type) =>
        type.Name == "Task" && type.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

    static bool IsNestedFunction(SyntaxNode node) =>
        node is SimpleLambdaExpressionSyntax or
            ParenthesizedLambdaExpressionSyntax or
            AnonymousMethodExpressionSyntax or
            LocalFunctionStatementSyntax;
}
