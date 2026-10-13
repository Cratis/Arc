// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.CodeFixes;
using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis.for_InlineGeneratedValueAnalyzer;

public class when_generation_cannot_be_moved_safely : given.an_inline_generation_analyzer
{
    [Theory]
    [InlineData("public CartId Handle(bool create) { if (create) return CartId.New(); return new CartId(Guid.Empty); }")]
    [InlineData("public CartId Handle() { for (var i = 0; i < 2; i++) { var id = CartId.New(); } return new CartId(Guid.Empty); }")]
    [InlineData("public virtual CartId Handle() => CartId.New();")]
    [InlineData("public System.Collections.Generic.IEnumerable<CartId> Handle(int[] values) => from value in values select CartId.New();")]
    [InlineData("public CartId? Handle(Allocate? other) => other?.Use(CartId.New()); public CartId Use(CartId id) => id;")]
    public async Task should_report_without_offering_a_fix(string method)
    {
        var project = TestProject.CreateProject("using System.Linq;\n" + Types + $"\n[Command] public record Allocate {{ {method} }}");
        var compilation = (await project.GetCompilationAsync())!;
        var diagnostics = await compilation.WithAnalyzers([new InlineGeneratedValueAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.Length.ShouldEqual(1);
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(project.Documents.Single(), diagnostics.Single(), (action, _) => actions.Add(action), CancellationToken.None);
        await new MoveToGeneratedValueCodeFixProvider().RegisterCodeFixesAsync(context);
        actions.ShouldBeEmpty();
    }
}
