// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.CodeFixes;
using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis.for_InlineGeneratedValueAnalyzer;

public class when_handle_implements_an_interface : given.an_inline_generation_analyzer
{
    [Fact]
    public async Task should_not_offer_a_signature_changing_fix()
    {
        var project = TestProject.CreateProject(Types + @"
            public interface IAllocator { CartId Handle(); }
            [Command] public record Allocate : IAllocator { public CartId Handle() => CartId.New(); }");
        var compilation = (await project.GetCompilationAsync())!;
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await compilation.WithAnalyzers([new InlineGeneratedValueAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.Length.ShouldEqual(1);
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(project.Documents.Single(), diagnostics.Single(), (action, _) => actions.Add(action), CancellationToken.None);
        await new MoveToGeneratedValueCodeFixProvider().RegisterCodeFixesAsync(context);
        actions.ShouldBeEmpty();
    }
}
