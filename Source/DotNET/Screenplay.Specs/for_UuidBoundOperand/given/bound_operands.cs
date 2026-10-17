// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_UuidBoundOperand.given;

internal static class bound_operands
{
    internal static UuidBoundOperand? Resolve(string arguments, string parameters = "Guid first, Guid second")
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            using System;
            public record ReserveBook(Guid Owner, Guid? Maybe)
            {
                public bool Reserve({{parameters}})
                {
                    Reserve({{arguments}});
                    return first == Owner;
                }
            }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == Analyzed.SlicePath);
        var model = compilation.GetSemanticModel(tree);
        var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        var method = (IMethodSymbol)model.GetSymbolInfo(call).Symbol!;
        var comparison = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

        return UuidBoundOperand.Resolve(comparison.Left, model, ParameterBindings.For(method, call, model));
    }
}
