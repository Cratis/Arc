// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RequireQueryAttributeCodeFixProvider;

public class when_a_partial_declaration_is_generated
{
    [Fact]
    public async Task should_refuse_a_whole_type_migration_that_cannot_edit_every_declaration()
    {
        var source = for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator.Source("public static Order Create(Unmarked e) => new();")
            .Replace("public record Order\n", "public partial record Order\n", StringComparison.Ordinal);
        var project = TestProject.CreateProject(source).AddAnalyzerReference(new GeneratedPartialReference());
        using var workspace = project.Solution.Workspace;
        var compilation = (await project.GetCompilationAsync(global::Xunit.TestContext.Current.CancellationToken))!;
        compilation.GetDiagnostics(global::Xunit.TestContext.Current.CancellationToken).Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        compilation.GetTypeByMetadataName("TestApp.Order")!.DeclaringSyntaxReferences.Length.ShouldEqual(2);
        var diagnostic = (await compilation.WithAnalyzers([new ReadModelCreatorAnalyzer()]).GetAnalyzerDiagnosticsAsync(global::Xunit.TestContext.Current.CancellationToken)).Single();
        var actions = new List<CodeAction>();
        await new CodeFixes.RequireQueryAttributeCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(
            project.Documents.Single(), diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        actions.ShouldBeEmpty();
    }

    sealed class GeneratedPartialReference : AnalyzerReference
    {
        public override string? FullPath => null;
        public override object Id => nameof(GeneratedPartialReference);
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => [];
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => [];
        public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => [new GeneratedPartial().AsSourceGenerator()];
    }

    sealed class GeneratedPartial : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) => context.RegisterPostInitializationOutput(
            _ => _.AddSource("Order.g.cs", "namespace TestApp; public partial record Order { public static Order ById(string id) => new(); }"));
    }
}
