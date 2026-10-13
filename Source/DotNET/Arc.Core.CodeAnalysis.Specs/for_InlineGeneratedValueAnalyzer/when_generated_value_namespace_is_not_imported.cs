// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.CodeFixes;
using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis.for_InlineGeneratedValueAnalyzer;

public class when_generated_value_namespace_is_not_imported : given.an_inline_generation_analyzer
{
    [Fact]
    public async Task should_add_the_using_and_simplify_the_attribute()
    {
        var source = Types.Replace("using Cratis.Arc.Commands.ModelBound;", string.Empty, StringComparison.Ordinal) + @"
            [global::Cratis.Arc.Commands.ModelBound.Command]
            public record Allocate { public CartId Handle() => CartId.New(); }";
        var project = TestProject.CreateProject(source);
        var compilation = (await project.GetCompilationAsync())!;
        var diagnostics = await compilation.WithAnalyzers([new InlineGeneratedValueAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        var document = project.Documents.Single();
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics.Single(), (action, _) => actions.Add(action), CancellationToken.None);
        await new MoveToGeneratedValueCodeFixProvider().RegisterCodeFixesAsync(context);
        var operations = await actions.Single().GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
        var root = (await changed.GetSyntaxRootAsync())!;
        root.ToString().ShouldContain("using Cratis.Arc.Commands.ModelBound;");
        var parameter = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Handle").ParameterList.Parameters.Single();
        parameter.AttributeLists.Single().Attributes.Single().Name.ToString().ShouldEqual("GeneratedValue");
        var output = (await changed.Project.GetCompilationAsync())!;
        output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }
}
