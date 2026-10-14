// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports statically invalid effects returned by invocation-local refusal callbacks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RefusalBranchReturnAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARCCHR0023_InvalidRefusalBranchReturn];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var invocationType = context.Compilation.GetTypeByMetadataName("Cratis.Arc.Chronicle.Reactors.CommandInvocation");
        if (invocation.TargetMethod.Name != "OnRefused" ||
            !(invocationType?.GetMembers("OnRefused").Any(method =>
                SymbolEqualityComparer.Default.Equals(method, invocation.TargetMethod.OriginalDefinition)) ?? false))
        {
            return;
        }

        var callback = invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 1);
        if (callback is null)
        {
            return;
        }

        var effects = new RefusalBranchEffects(context.Compilation);
        foreach (var value in RefusalCallbackReturns.Read(callback.Value, context.Compilation, context.CancellationToken))
        {
            if (effects.IsInvalid(value))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARCCHR0023_InvalidRefusalBranchReturn,
                    value.Syntax.GetLocation()));
            }
        }
    }
}
