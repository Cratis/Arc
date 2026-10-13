// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using Verify = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.InlineGeneratedValueAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_InlineGeneratedValueAnalyzer;

public class when_generating_inline : given.an_inline_generation_analyzer
{
    [Theory]
    [InlineData("CartId.New()")]
    [InlineData("new CartId(GenerateValue.Uuid())")]
    [InlineData("new CartId(Guid.NewGuid())")]
    [InlineData("(CartId)Guid.NewGuid()")]
    [InlineData("GenerateValue.Uuid()")]
    public async Task should_suggest_a_generated_parameter(string expression) => await Verify.VerifyAnalyzerAsync(
        Types + $"\n[Command] public record Allocate {{ public CartId Handle() => {{|#0:{expression}|}}; }}",
        Verify.Diagnostic("ARC0028").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("CartId"));

    [Theory]
    [InlineData("public CartId Other() => CartId.New();")]
    [InlineData("public static CartId Handle() => CartId.New();")]
    [InlineData("public Guid Handle() => Guid.NewGuid();")]
    [InlineData("public CartId Handle() { Func<CartId> create = () => CartId.New(); return create(); }")]
    [InlineData("public CartId Handle() { CartId Create() => CartId.New(); return Create(); }")]
    public async Task should_ignore_other_generation_locations(string method) => await Verify.VerifyAnalyzerAsync(Types + $"\n[Command] public record Allocate {{ {method} }}");

    [Fact] public async Task should_ignore_non_commands() => await Verify.VerifyAnalyzerAsync(Types + "\npublic record Helper { public CartId Handle() => CartId.New(); }");
}
