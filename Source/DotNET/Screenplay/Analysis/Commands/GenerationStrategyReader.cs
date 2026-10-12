// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves the UUID v4 generation contract without guessing from a factory's name.
/// </summary>
/// <param name="models">The models resolving concept bodies.</param>
public class GenerationStrategyReader(SemanticModels models)
{
    /// <summary>
    /// Reads a generated parameter's factory and explains any unrecovered strategy.
    /// </summary>
    /// <param name="parameter">The generated Handle parameter.</param>
    /// <returns>Null for proven UUID v4 generation, otherwise the reason it remains in code.</returns>
    public string? Failure(IParameterSymbol parameter)
    {
        var type = parameter.Type;
        if (parameter.RefKind != RefKind.None || type.NullableAnnotation == NullableAnnotation.Annotated ||
            type is not INamedTypeSymbol named)
        {
            return "not a required scalar UUID concept";
        }

        var contract = named.AllInterfaces.SingleOrDefault(candidate => candidate.Is(WellKnownTypeNames.Generatable) &&
            candidate.TypeArguments is [var self] && SymbolEqualityComparer.Default.Equals(self, named));
        var member = contract?.GetMembers("New").OfType<IMethodSymbol>().SingleOrDefault();
        var factory = member is null ? null : named.FindImplementationForInterfaceMember(member) as IMethodSymbol;
        if (factory is null || factory.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().ToArray() is not [var declaration] ||
            models.For(declaration.SyntaxTree) is not { } model)
        {
            return "New() has no readable IGeneratable<T> implementation";
        }

        var returned = declaration.ExpressionBody?.Expression ??
            (declaration.Body?.Statements is [ReturnStatementSyntax statement] ? statement.Expression : null);
        if (named.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is [var backing] && backing.Is("System.Guid") &&
            returned is not null && CreatesUuid(returned, named, model))
        {
            return null;
        }

        var calls = declaration.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => model.GetSymbolInfo(call).Symbol).OfType<IMethodSymbol>().ToList();
        if (calls.Exists(call => call.Name == "UuidV7" || call.Name == "CreateVersion7"))
        {
            return "uuidv7 strategy";
        }
        if (calls.Exists(call => call.Name == "GetHexString" && call.ContainingType.Is("System.Security.Cryptography.RandomNumberGenerator")))
        {
            return "hex-bytes strategy";
        }

        return "New() body is not a recognized UUID v4 form";
    }

    /// <summary>
    /// Proves a constructor forwards a UUID unchanged to the concept base.
    /// </summary>
    /// <param name="constructor">The constructor to follow.</param>
    /// <param name="visited">Constructors already followed.</param>
    /// <returns>Whether the UUID is preserved.</returns>
    public bool ForwardsUuid(IMethodSymbol constructor, ISet<IMethodSymbol> visited)
    {
        if (!visited.Add(constructor) || constructor.Parameters is not [var parameter] || !parameter.Type.Is("System.Guid"))
        {
            return false;
        }

        if ((constructor.ContainingType.Is(WellKnownTypeNames.ConceptAs) || constructor.ContainingType.Is(WellKnownTypeNames.EventSourceIdOfT)) &&
            constructor.ContainingAssembly.Name.StartsWith("Cratis.", StringComparison.Ordinal))
        {
            return true;
        }

        return constructor.DeclaringSyntaxReferences.Any(reference =>
        {
            var initializer = reference.GetSyntax() switch
            {
                RecordDeclarationSyntax record => (SyntaxNode?)record.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault(),
                ConstructorDeclarationSyntax { Body: null or { Statements.Count: 0 }, ExpressionBody: null } explicitConstructor => explicitConstructor.Initializer,
                _ => null
            };
            var arguments = initializer switch
            {
                PrimaryConstructorBaseTypeSyntax primary => primary.ArgumentList,
                ConstructorInitializerSyntax explicitInitializer => explicitInitializer.ArgumentList,
                _ => null
            };

            return arguments?.Arguments is [var argument] && models.For(initializer!.SyntaxTree) is { } model &&
                model.GetSymbolInfo(MappingSourceReader.Unwrap(argument.Expression)).Symbol is IParameterSymbol forwarded &&
                !argument.Expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax) && model.GetConversion(argument.Expression).IsIdentity &&
                SymbolEqualityComparer.Default.Equals(forwarded, parameter) && model.GetSymbolInfo(initializer).Symbol is IMethodSymbol target && ForwardsUuid(target, visited);
        });
    }

    static bool IsUuidCall(ExpressionSyntax expression, SemanticModel model) => expression is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } call &&
        model.GetSymbolInfo(call).Symbol is IMethodSymbol { IsStatic: true, Parameters.Length: 0 } method &&
        ((method.Name == "NewGuid" && method.ContainingType.Is("System.Guid")) ||
         (method.Name == "Uuid" && method.ContainingType.Is("Cratis.Concepts.GenerateValue")));

    bool CreatesUuid(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        if (expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax))
        {
            return false;
        }

        if (expression is BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation)
        {
            return model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor && SymbolEqualityComparer.Default.Equals(constructor.ContainingType, type) &&
                ForwardsUuid(constructor, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default)) && IsUuidCall(argument.Expression, model);
        }

        var conversion = model.GetConversion(expression);
        return IsUuidCall(expression, model) && conversion.MethodSymbol is { Parameters: [var parameter] } method &&
            SymbolEqualityComparer.Default.Equals(method.ReturnType, type) && method.DeclaringSyntaxReferences.Any(reference =>
            {
                if (reference.GetSyntax() is not ConversionOperatorDeclarationSyntax declaration || models.For(declaration.SyntaxTree) is not { } conversionModel)
                {
                    return false;
                }

                var returned = declaration.ExpressionBody?.Expression ??
                    (declaration.Body?.Statements is [ReturnStatementSyntax statement] ? statement.Expression : null);

                return returned is BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation &&
                    conversionModel.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor && SymbolEqualityComparer.Default.Equals(constructor.ContainingType, type) &&
                    ForwardsUuid(constructor, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default)) && conversionModel.GetConversion(argument.Expression).IsIdentity &&
                    SymbolEqualityComparer.Default.Equals(conversionModel.GetSymbolInfo(argument.Expression).Symbol, parameter);
            });
    }
}
