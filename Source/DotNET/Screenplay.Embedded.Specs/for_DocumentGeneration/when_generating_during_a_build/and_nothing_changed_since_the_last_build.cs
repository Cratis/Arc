// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

/// <summary>
/// Analyzing the whole application is the expensive part of a build, so a build that nothing the documents are
/// generated from has changed since has to hand back what the last one produced, rather than produce it again.
/// </summary>
public class and_nothing_changed_since_the_last_build : Specification, IDisposable
{
    const string AssemblyName = "Library";

    string _directory;
    string _output;
    string _source;
    GenerateEmbeddedScreenplayDocuments _first;
    GenerateEmbeddedScreenplayDocuments _second;
    given.a_build _secondBuild;
    bool _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-screenplay-{Guid.NewGuid():N}");
        _output = Path.Combine(_directory, "generated");
        Directory.CreateDirectory(_directory);
        _source = Path.Combine(_directory, "Program.cs");
        File.WriteAllText(_source, "namespace Library;\n\npublic static class Program\n{\n    public static void Main() { }\n}\n");

        _first = TaskFor(new());
        _first.Execute();

        _secondBuild = new();
        _second = TaskFor(_secondBuild);
    }

    void Because() => _result = _second.Execute();

    void Destroy() => Dispose();

    static IEnumerable<string> Platform() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

    GenerateEmbeddedScreenplayDocuments TaskFor(given.a_build build) => new()
    {
        BuildEngine = build,
        AssemblyName = AssemblyName,
        RootNamespace = AssemblyName,
        LanguageVersion = "latest",
        DefineConstants = "TRACE;DEBUG",
        OutputPath = _output,
        Sources = [new TaskItem(_source)],
        References = [.. Platform().Select(_ => (ITaskItem)new TaskItem(_))]
    };

    [Fact] void should_succeed() => _result.ShouldBeTrue();

    [Fact] void should_hand_back_the_same_files() =>
        _second.Documents.Select(_ => _.ItemSpec).ShouldContainOnly(_first.Documents.Select(_ => _.ItemSpec));

    [Fact] void should_hand_back_the_same_logical_names() =>
        _second.Documents.Select(_ => _.GetMetadata("LogicalName")).ShouldContainOnly(_first.Documents.Select(_ => _.GetMetadata("LogicalName")));

    [Fact] void should_say_that_it_reused_what_it_embedded_before() =>
        _secondBuild.Messages.Exists(_ => _.Message!.Contains("are reused", StringComparison.Ordinal)).ShouldBeTrue();

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
