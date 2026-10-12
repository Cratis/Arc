// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.CodeFixes;
using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

using Verify = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.InlineGeneratedValueAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_InlineGeneratedValueAnalyzer;

public class when_moving_generation_to_a_parameter : given.an_inline_generation_analyzer
{
    [Theory]
    [InlineData("CartId.New()")]
    [InlineData("new CartId(Guid.NewGuid())")]
    [InlineData("GenerateValue.Uuid()")]
    public async Task should_move_the_whole_expression(string expression) => await CodeFixVerifier<InlineGeneratedValueAnalyzer, MoveToGeneratedValueCodeFixProvider>.VerifyCodeFixAsync(
        Types + $"\n[Command] public record Allocate {{ public CartId Handle() => {{|#0:{expression}|}}; }}",
        Types + "\n[Command] public record Allocate { public CartId Handle([global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute] global::CartId cartId) => cartId; }",
        Verify.Diagnostic("ARC0028").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("CartId"));
}
