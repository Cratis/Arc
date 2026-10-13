// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves concept generation strategies without guessing from a factory's name.
/// </summary>
/// <param name="models">The models resolving concept bodies.</param>
internal class GenerationStrategyReader(SemanticModels models)
{
    readonly UuidGenerationForms _forms = new(models);

    /// <summary>
    /// Reads a generated parameter's factory and explains any unrecovered strategy.
    /// </summary>
    /// <param name="parameter">The generated Handle parameter.</param>
    /// <returns>Null for proven generation, otherwise the reason it remains in code.</returns>
    public string? Failure(IParameterSymbol parameter)
    {
        if (!IsRequiredScalar(parameter) || parameter.Type is not INamedTypeSymbol type)
        {
            return "not a required scalar generatable concept";
        }
        if (!HasSupportedBacking(type))
        {
            return "backing type is not UUID or String";
        }
        if (FactoryBodyOf(type) is not { } factory)
        {
            return "New() has no readable IGeneratable<T> implementation or IGeneratableInContext<T> implementation";
        }

        return ReadFactory(type, factory) is null ? "New() body is not a recognized generation form or its literal strategy arguments are invalid" : null;
    }

    /// <summary>
    /// Reads the strategy owned by a concept's generation contract.
    /// </summary>
    /// <param name="type">The concept type.</param>
    /// <returns>The strategy, or null for an unrecognized factory.</returns>
    public GenerationModel? Read(INamedTypeSymbol type) => FactoryBodyOf(type) is { } factory ? ReadFactory(type, factory) : null;

    /// <summary>
    /// Reads an inline factory expression using the same proof as explicit generation.
    /// </summary>
    /// <param name="expression">The directly returned or local expression.</param>
    /// <param name="type">The expected concept.</param>
    /// <param name="model">The model resolving the expression.</param>
    /// <returns>The proven generation strategy, or null.</returns>
    public GenerationModel? Read(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model) => HasSupportedBacking(type) ? _forms.Strategy(expression, type, model) : null;

    /// <summary>
    /// Reads a legacy local or response through the same generation proof.
    /// </summary>
    /// <param name="expression">The local initializer or returned expression.</param>
    /// <param name="type">The expected concept.</param>
    /// <param name="model">The model resolving the expression.</param>
    /// <returns>Whether its strategy is implicit UUID or matches the concept's contract.</returns>
    public bool ReadLocal(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        var strategy = Read(expression, type, model) ?? LocalFactory(expression, type, model);
        if (strategy is null)
        {
            return false;
        }

        return strategy.Kind == "uuid" || strategy == Read(type);
    }

    static bool IsRequiredScalar(IParameterSymbol parameter) => parameter.RefKind == RefKind.None && parameter.Type.NullableAnnotation != NullableAnnotation.Annotated;

    static bool IsSelfContract(INamedTypeSymbol contract, INamedTypeSymbol type) => IsGenerationContract(contract) && HasSelfArgument(contract, type);

    static bool IsGenerationContract(INamedTypeSymbol contract) => contract.Is(WellKnownTypeNames.Generatable) || contract.Is("Cratis.Concepts.IGeneratableInContext`1");

    static bool HasSelfArgument(INamedTypeSymbol contract, INamedTypeSymbol type) => contract.TypeArguments is [var self] && SymbolEqualityComparer.Default.Equals(self, type);

    static bool HasSupportedBacking(INamedTypeSymbol type) => type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is [var backing] && (backing.Is("System.Guid") || backing.SpecialType == SpecialType.System_String);

    static IMethodSymbol? FactoryOf(INamedTypeSymbol type)
    {
        var contract = type.AllInterfaces.Where(candidate => IsSelfContract(candidate, type)).OrderByDescending(candidate => candidate.Is("Cratis.Concepts.IGeneratableInContext`1")).FirstOrDefault();
        var member = contract?.GetMembers("New").OfType<IMethodSymbol>().SingleOrDefault();

        return member is null ? null : type.FindImplementationForInterfaceMember(member) as IMethodSymbol;
    }

    static bool IsLocalFactory(IMethodSymbol method, INamedTypeSymbol type) =>
        method.IsStatic && method.Name == "New" && method.Parameters.IsEmpty &&
        SymbolEqualityComparer.Default.Equals(method.ReturnType, type) && SymbolEqualityComparer.Default.Equals(method.ContainingType, type);

    FactoryBody? FactoryBodyOf(INamedTypeSymbol type)
    {
        var declarations = FactoryOf(type)?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().ToArray();
        if (declarations is not [var declaration] || models.For(declaration.SyntaxTree) is not { } model)
        {
            return null;
        }

        return new(declaration, model);
    }

    GenerationModel? LocalFactory(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax call)
        {
            return null;
        }
        if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method || !IsLocalFactory(method, type))
        {
            return null;
        }
        var declarations = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().ToArray();
        if (declarations is not [var declaration] || models.For(declaration.SyntaxTree) is not { } factoryModel)
        {
            return null;
        }

        return ReadFactory(type, new(declaration, factoryModel));
    }

    GenerationModel? ReadFactory(INamedTypeSymbol type, FactoryBody factory) => UuidGenerationForms.ReturnedBy(factory.Declaration) is { } returned ? Read(returned, type, factory.Model) : null;

    sealed record FactoryBody(MethodDeclarationSyntax Declaration, SemanticModel Model);
}
