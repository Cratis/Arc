// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves the UUID v4 generation contract without guessing from a factory's name.
/// </summary>
/// <param name="models">The models resolving concept bodies.</param>
internal class GenerationStrategyReader(SemanticModels models)
{
    readonly UuidGenerationForms _forms = new(models);

    /// <summary>
    /// Reads a generated parameter's factory and explains any unrecovered strategy.
    /// </summary>
    /// <param name="parameter">The generated Handle parameter.</param>
    /// <returns>Null for proven UUID v4 generation, otherwise the reason it remains in code.</returns>
    public string? Failure(IParameterSymbol parameter)
    {
        if (!IsRequiredScalar(parameter) || parameter.Type is not INamedTypeSymbol type)
        {
            return "not a required scalar UUID concept";
        }
        if (type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is [var backing] && !backing.Is("System.Guid"))
        {
            return $"backing type '{backing.ToDisplayString()}' is not UUID";
        }
        if (FactoryBodyOf(type) is not { } factory)
        {
            return "New() has no readable IGeneratable<T> implementation";
        }
        if (IsUuidFactory(type, factory))
        {
            return null;
        }

        return UnrecognizedStrategy(factory);
    }

    static bool IsRequiredScalar(IParameterSymbol parameter) => parameter.RefKind == RefKind.None && parameter.Type.NullableAnnotation != NullableAnnotation.Annotated;

    static bool IsSelfContract(INamedTypeSymbol contract, INamedTypeSymbol type) => contract.Is(WellKnownTypeNames.Generatable) && HasSelfArgument(contract, type);

    static bool HasSelfArgument(INamedTypeSymbol contract, INamedTypeSymbol type) => contract.TypeArguments is [var self] && SymbolEqualityComparer.Default.Equals(self, type);

    static bool IsUuidConcept(INamedTypeSymbol type) => type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is [var backing] && backing.Is("System.Guid");

    static string UnrecognizedStrategy(FactoryBody factory)
    {
        var calls = factory.Declaration.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => factory.Model.GetSymbolInfo(call).Symbol).OfType<IMethodSymbol>().ToList();
        if (calls.Exists(IsUuidV7))
        {
            return "uuidv7 strategy";
        }

        return calls.Exists(IsHexBytes) ? "hex-bytes strategy" : "New() body is not a recognized UUID v4 form";
    }

    static bool IsUuidV7(IMethodSymbol method) => method.Name == "UuidV7" || method.Name == "CreateVersion7";

    static bool IsHexBytes(IMethodSymbol method) => method.Name == "GetHexString" && method.ContainingType.Is("System.Security.Cryptography.RandomNumberGenerator");

    static IMethodSymbol? FactoryOf(INamedTypeSymbol type)
    {
        var contract = type.AllInterfaces.SingleOrDefault(candidate => IsSelfContract(candidate, type));
        var member = contract?.GetMembers("New").OfType<IMethodSymbol>().SingleOrDefault();

        return member is null ? null : type.FindImplementationForInterfaceMember(member) as IMethodSymbol;
    }

    FactoryBody? FactoryBodyOf(INamedTypeSymbol type)
    {
        var declarations = FactoryOf(type)?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().ToArray();
        if (declarations is not [var declaration] || models.For(declaration.SyntaxTree) is not { } model)
        {
            return null;
        }

        return new(declaration, model);
    }

    bool IsUuidFactory(INamedTypeSymbol type, FactoryBody factory)
    {
        if (!IsUuidConcept(type) || UuidGenerationForms.ReturnedBy(factory.Declaration) is not { } returned)
        {
            return false;
        }

        return _forms.Read(returned, type, factory.Model);
    }

    sealed record FactoryBody(MethodDeclarationSyntax Declaration, SemanticModel Model);
}
