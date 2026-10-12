// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RequireQueryAttributeCodeFixProvider;

public class when_migrating_partial_query_methods
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public async Task should_mark_one_logical_query_without_changing_either_part(bool separateDocument, bool markedDefinition, bool markedImplementation)
    {
        var definition = $"{(markedDefinition ? "[Query] " : string.Empty)}[Path(\"/orders\")] public static partial Order ById(string id);";
        var implementation = $"{(markedImplementation ? "[Query] " : string.Empty)}public static partial Order ById(string id) => new();";
        var source = for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator.Source($"public static Order Create(Unmarked e) => new(); {definition} {(separateDocument ? string.Empty : implementation)}")
            .Replace("public record Order\n", "public partial record Order\n", StringComparison.Ordinal);
        var project = TestProject.CreateProject(source);
        using var workspace = project.Solution.Workspace;
        var document = project.Documents.Single();
        if (separateDocument)
        {
            project = project.AddDocument("Other.cs", SourceText.From($"using Cratis.Arc.Queries.ModelBound; namespace TestApp; public partial record Order {{ {implementation} }}")).Project;
        }

        var compilation = (await project.GetCompilationAsync())!;
        compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostic = (await compilation.WithAnalyzers([new ReadModelCreatorAnalyzer()]).GetAnalyzerDiagnosticsAsync()).Single();
        var actions = new List<CodeAction>();
        await new CodeFixes.RequireQueryAttributeCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(
            project.GetDocument(document.Id)!, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        var operations = await actions.Single().GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var output = (await changed.GetProject(project.Id)!.GetCompilationAsync())!;
        output.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var method = output.GetTypeByMetadataName("TestApp.Order")!.GetMembers("ById").OfType<IMethodSymbol>().Single();
        method.GetAttributes().Select(_ => _.AttributeClass!.Name).ShouldContainOnly("PathAttribute", "QueryAttribute");
        method.PartialImplementationPart.ShouldNotBeNull();
        var roots = await Task.WhenAll(changed.GetProject(project.Id)!.Documents.Select(_ => _.GetSyntaxRootAsync()));
        roots.SelectMany(_ => _!.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>())
            .Count(_ => _.Name.ToString().EndsWith("Query", StringComparison.Ordinal)).ShouldEqual(1);
    }

    [Fact]
    public async Task should_refuse_a_migration_that_introduces_compiler_errors()
    {
        var source = for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator.Source("public static Order Create(Unmarked e) => new(); public static Order ById(string id) => new();") +
            "\nnamespace Cratis.Arc.Queries.ModelBound { public class ReadModelAttribute : System.Attribute; }";
        var project = TestProject.CreateProject(source);
        using var workspace = project.Solution.Workspace;
        var compilation = (await project.GetCompilationAsync())!;
        compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostic = (await compilation.WithAnalyzers([new ReadModelCreatorAnalyzer()]).GetAnalyzerDiagnosticsAsync()).Single();
        var actions = new List<CodeAction>();
        await new CodeFixes.RequireQueryAttributeCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(
            project.Documents.Single(), diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_preserve_unrelated_preexisting_errors()
    {
        var source = for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator.Source("public static Order Create(Unmarked e) => new(); public static Order ById(string id) => new();") +
            "\nnamespace TestApp { public class Broken { Missing Value; } }";
        var project = TestProject.CreateProject(source);
        using var workspace = project.Solution.Workspace;
        var compilation = (await project.GetCompilationAsync())!;
        var diagnostic = (await compilation.WithAnalyzers([new ReadModelCreatorAnalyzer()]).GetAnalyzerDiagnosticsAsync()).Single();
        var actions = new List<CodeAction>();
        await new CodeFixes.RequireQueryAttributeCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(
            project.Documents.Single(), diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        actions.Count.ShouldEqual(1);
    }
}
