// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.Build;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

/// <summary>
/// The task is what a consuming project really runs, so it is specified the way MSBuild runs it - real files on
/// disk, real references, a build engine listening. What it hands back has to be items the compiler can embed,
/// each carrying the logical name the host will later look the resource up by.
/// </summary>
public class for_a_project : Specification, IDisposable
{
    const string AssemblyName = "Library";

    string _directory;
    string _output;
    given.a_build _build;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-screenplay-{Guid.NewGuid():N}");
        _output = Path.Combine(_directory, "generated");
        Directory.CreateDirectory(_directory);

        var source = Path.Combine(_directory, "Program.cs");
        File.WriteAllText(source, "namespace Library;\n\npublic static class Program\n{\n    public static void Main() { }\n}\n");

        _build = new();
        _task = new()
        {
            BuildEngine = _build,
            AssemblyName = AssemblyName,
            RootNamespace = AssemblyName,
            LanguageVersion = "latest",
            DefineConstants = "TRACE;DEBUG",
            OutputPath = _output,
            Sources = [new TaskItem(source)],
            References = [.. Platform().Select(_ => (ITaskItem)new TaskItem(_))]
        };
    }

    void Because() => _result = _task.Execute();

    static IEnumerable<string> Platform() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

    ITaskItem Catalog => _task.Documents.First(_ => _.GetMetadata("LogicalName") == EmbeddedResourceNames.Catalog);

    [Fact] void should_succeed() => _result.ShouldBeTrue();

    [Fact] void should_report_nothing_as_an_error() => _build.Errors.ShouldBeEmpty();

    [Fact] void should_hand_back_a_document_and_a_catalog() => _task.Documents.Length.ShouldEqual(2);

    [Fact] void should_embed_the_document_of_the_assembly_under_its_namespace() =>
        _task.Documents.Select(_ => _.GetMetadata("LogicalName"))
            .ShouldContain($"Cratis.Arc.Screenplay.Embedded.documents.{AssemblyName}.play");

    [Fact] void should_embed_the_catalog_under_the_name_the_host_reads_it_by() =>
        _task.Documents.Select(_ => _.GetMetadata("LogicalName")).ShouldContain(EmbeddedResourceNames.Catalog);

    [Fact] void should_write_every_file_it_handed_back() =>
        _task.Documents.All(_ => File.Exists(_.ItemSpec)).ShouldBeTrue();

    [Fact] void should_write_into_the_directory_the_build_asked_for() =>
        _task.Documents.All(_ => _.ItemSpec.StartsWith(_output, StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] void should_write_a_catalog_naming_the_assembly_document() =>
        JsonDocument.Parse(File.ReadAllText(Catalog.ItemSpec))
            .RootElement.GetProperty("documents")[0].GetProperty("id").GetString()
            .ShouldEqual(AssemblyName);

    [Fact] void should_write_a_document_the_screenplay_compiler_accepts() =>
        new Cratis.Screenplay.ScreenplayCompiler()
            .Compile(File.ReadAllText(_task.Documents.First(_ => _.ItemSpec.EndsWith(".play", StringComparison.Ordinal)).ItemSpec))
            .Success
            .ShouldBeTrue();

    [Fact] void should_leave_nothing_else_behind_in_the_directory() =>
        Directory.GetFiles(_output).Length.ShouldEqual(2);

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
