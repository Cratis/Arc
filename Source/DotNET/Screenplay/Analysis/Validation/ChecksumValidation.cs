// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Recognizes the exact unconditional Fundamentals checksum predicate on a String concept's value.
/// </summary>
internal static class ChecksumValidation
{
    static readonly Dictionary<int, string> _algorithms = new() { [1] = "luhn", [2] = "mod11", [3] = "mod97" };

    /// <summary>
    /// Reads an algorithm without evaluating or approximating arbitrary predicates.
    /// </summary>
    /// <param name="call">The Must call.</param>
    /// <param name="model">The model resolving its predicate.</param>
    /// <returns>The portable algorithm name, or null.</returns>
    internal static string? Read(InvocationExpressionSyntax call, SemanticModel model)
    {
        if (!IsStringConcept(call, model))
        {
            return null;
        }
        if (call.ArgumentList.Arguments is not [{ Expression: SimpleLambdaExpressionSyntax lambda }])
        {
            return null;
        }

        return ReadPredicate(lambda, model);
    }

    static string? ReadPredicate(SimpleLambdaExpressionSyntax lambda, SemanticModel model)
    {
        if (lambda.ExpressionBody is not InvocationExpressionSyntax body || model.GetOperation(body) is not IInvocationOperation invocation)
        {
            return null;
        }
        if (!IsChecksumCall(invocation) || !UsesValue(invocation, model.GetDeclaredSymbol(lambda.Parameter)))
        {
            return null;
        }

        return AlgorithmOf(invocation.Arguments[1].Value);
    }

    static bool IsStringConcept(SyntaxNode call, SemanticModel model)
    {
        var validator = model.GetEnclosingSymbol(call.SpanStart)?.ContainingType;
        var concept = ValidationReader.ValidatedTypeOf(validator!)?.FindBase(WellKnownTypeNames.ConceptAs);

        return concept?.TypeArguments is [{ SpecialType: SpecialType.System_String }];
    }

    static bool IsChecksumCall(IInvocationOperation call) => call.Arguments.Length == 2 && IsChecksumMethod(call.TargetMethod);

    static bool IsChecksumMethod(IMethodSymbol method) => method.Name == "HasValidChecksum" &&
        method.DeclaringSyntaxReferences.IsEmpty && method.ContainingType.Is("Cratis.Concepts.GenerateValue");

    static bool UsesValue(IInvocationOperation call, ISymbol? parameter) => call.Arguments[0].Value is IParameterReferenceOperation reference &&
        SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);

    static string? AlgorithmOf(IOperation value)
    {
        if (value is not IFieldReferenceOperation field || !field.Field.ContainingType.Is("Cratis.Concepts.Checksum"))
        {
            return null;
        }

        return field.ConstantValue.Value is int algorithm ? _algorithms.GetValueOrDefault(algorithm) : null;
    }
}
