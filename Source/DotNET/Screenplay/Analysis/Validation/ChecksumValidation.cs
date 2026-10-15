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
    /// <summary>
    /// Reads an algorithm without evaluating or approximating arbitrary predicates.
    /// </summary>
    /// <param name="call">The Must call.</param>
    /// <param name="model">The model resolving its predicate.</param>
    /// <returns>The portable algorithm name, or null.</returns>
    internal static string? Read(InvocationExpressionSyntax call, SemanticModel model)
    {
        if (call.ArgumentList.Arguments is not [{ Expression: SimpleLambdaExpressionSyntax lambda }] ||
            lambda.ExpressionBody is not InvocationExpressionSyntax body || model.GetOperation(body) is not IInvocationOperation invocation ||
            !invocation.TargetMethod.ContainingType.Is("Cratis.Concepts.GenerateValue") || invocation.TargetMethod.Name != "HasValidChecksum" ||
            !invocation.TargetMethod.DeclaringSyntaxReferences.IsEmpty || invocation.Arguments.Length != 2)
        {
            return null;
        }
        var parameter = model.GetDeclaredSymbol(lambda.Parameter);
        var value = invocation.Arguments[0].Value;
        var checksum = invocation.Arguments[1].Value;
        var validator = model.GetEnclosingSymbol(call.SpanStart)?.ContainingType;
        var concept = ValidationReader.ValidatedTypeOf(validator!)?.FindBase(WellKnownTypeNames.ConceptAs);
        if (concept?.TypeArguments is not [{ SpecialType: SpecialType.System_String }] ||
            value is not IParameterReferenceOperation reference || !SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter) ||
            checksum is not IFieldReferenceOperation field || !field.Field.ContainingType.Is("Cratis.Concepts.Checksum"))
        {
            return null;
        }

        return field.ConstantValue.Value switch { 1 => "luhn", 2 => "mod11", 3 => "mod97", _ => null };
    }
}
