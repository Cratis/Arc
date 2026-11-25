// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator;

public class when_an_unrelated_file_changes : Specification
{
    GeneratorDriverRunResult _before;
    GeneratorDriverRunResult _after;
    GeneratorDriver _driver;
    CSharpCompilation _compilation;
    SyntaxTree _unrelated;

    void Establish()
    {
        var declarations = CSharpSyntaxTree.ParseText("""
            using Owner = Cratis.Arc.Queries.ModelBound.ReadModelAttribute;
            using Cratis.Arc.Queries.ModelBound;
            public partial class First { public static First All() => new(); }
            [ReadModel] public class Second { public static Second All() => new(); }
            [Owner(RequireQueryAttribute = true)] public partial class First { [Query] public static First Selected() => new(); }
            public class Derived : First { [Query] public static Derived SelectedAgain() => new(); }
            public class NotAnOwner { [Query] private static NotAnOwner Invalid() => new(); }
            namespace Cratis.Arc.Queries.ModelBound
            {
                public class ReadModelAttribute : System.Attribute { public bool RequireQueryAttribute { get; set; } }
                public class QueryAttribute : System.Attribute { }
            }
            """);
        _unrelated = CSharpSyntaxTree.ParseText("public class Unrelated { }");
        _compilation = CSharpCompilation.Create(
            "Changing",
            [declarations, _unrelated],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new QueryMetadataGenerator().AsSourceGenerator(), new ModelGraphWalkerGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        _driver = driver.RunGenerators(_compilation);
        _before = _driver.GetRunResult();
    }

    void Because() => _after = _driver.RunGenerators(_compilation.ReplaceSyntaxTree(_unrelated,
        CSharpSyntaxTree.ParseText("public class Unrelated { public int Value { get; set; } }"))).GetRunResult();

    [Fact] void should_cache_query_metadata() => ReasonsFor("ReadModelMetadata").ShouldEachConformTo(IsUnchanged);
    [Fact] void should_cache_walker_roots() => ReasonsFor("ReadModelRoots").ShouldEachConformTo(IsUnchanged);
    [Fact] void should_track_all_query_models() => ReasonsFor("ReadModelMetadata").Count().ShouldEqual(3);
    [Fact] void should_track_all_walker_roots() => ReasonsFor("ReadModelRoots").Count().ShouldEqual(3);
    [Fact] void should_preserve_generated_text() => Sources(_after).ShouldEqual(Sources(_before));
    [Fact] void should_validate_a_marker_without_an_owner() => _after.Diagnostics.Count(diagnostic => diagnostic.Id == "ARC0026").ShouldEqual(1);
    [Fact] void should_keep_inherited_query_selection() => Sources(_after).ShouldContain("Derived.SelectedAgain");
    [Fact] void should_keep_partial_declaration_order() => (Sources(_after).IndexOf("First.Selected", StringComparison.Ordinal) < Sources(_after).IndexOf("Second.All", StringComparison.Ordinal)).ShouldBeTrue();

    IEnumerable<IncrementalStepRunReason> ReasonsFor(string name) => _after.Results
        .SelectMany(result => result.TrackedSteps.TryGetValue(name, out var steps) ? steps : [])
        .SelectMany(step => step.Outputs.Select(output => output.Reason));

    static bool IsUnchanged(IncrementalStepRunReason reason) => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged;

    static string Sources(GeneratorDriverRunResult result) => string.Join('\n', result.Results.SelectMany(generator => generator.GeneratedSources).Select(source => source.SourceText.ToString()));
}
