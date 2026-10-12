// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RequireQueryAttributeCodeFixProvider;

public class when_migrating_partial_read_models
{
    [Fact]
    public async Task should_migrate_every_file_without_marking_the_fold()
    {
        var source = for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator.Source("public static Order Create(Unmarked e) => new();")
            .Replace("public record Order\n", "public partial record Order\n", StringComparison.Ordinal);
        var project = TestProject.CreateProject(source);
        using var workspace = project.Solution.Workspace;
        var document = project.Documents.Single();
        project = project.AddDocument("Other.cs", SourceText.From("using Cratis.Arc.Queries.ModelBound; namespace TestApp; public partial record Order { [Path(\"/orders\")] public static Order ById(string id) => new(); }")).Project;
        var compilation = (await project.GetCompilationAsync())!;
        var diagnostic = (await compilation.WithAnalyzers([new ReadModelCreatorAnalyzer()]).GetAnalyzerDiagnosticsAsync()).Single();
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(project.GetDocument(document.Id)!, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await new CodeFixes.RequireQueryAttributeCodeFixProvider().RegisterCodeFixesAsync(context);
        var operations = await actions.Single().GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var output = (await changed.GetProject(project.Id)!.GetCompilationAsync())!;
        output.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var model = output.GetTypeByMetadataName("TestApp.Order")!;
        model.GetAttributes().Single(_ => _.AttributeClass!.Name == "ReadModelAttribute").NamedArguments.Single().Value.Value.ShouldEqual(true);
        var methods = model.GetMembers().OfType<IMethodSymbol>().Where(_ => _.MethodKind == MethodKind.Ordinary).ToArray();
        methods.Single(_ => _.Name == "Create").GetAttributes().ShouldBeEmpty();
        methods.Single(_ => _.Name == "ById").GetAttributes().Select(_ => _.AttributeClass!.Name).ShouldContainOnly("PathAttribute", "QueryAttribute");
    }
}
