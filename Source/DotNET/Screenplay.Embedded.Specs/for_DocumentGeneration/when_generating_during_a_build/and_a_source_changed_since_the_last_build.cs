// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

/// <summary>
/// What the documents are generated from changing is the whole reason they have to be generated again.
/// </summary>
public class and_a_source_changed_since_the_last_build : Specification, IDisposable
{
    const string AssemblyName = "Library";

    string _directory;
    string _output;
    string _source;
    GenerateEmbeddedScreenplayDocuments _first;
    GenerateEmbeddedScreenplayDocuments _second;
    given.a_build _secondBuild;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-screenplay-{Guid.NewGuid():N}");
        _output = Path.Combine(_directory, "generated");
        Directory.CreateDirectory(_directory);
        _source = Path.Combine(_directory, "Program.cs");
        File.WriteAllText(_source, "namespace Library;\n\npublic static class Program\n{\n    public static void Main() { }\n}\n");

        _first = TaskFor(new());
        _first.Execute();

        File.WriteAllText(_source, "namespace Library;\n\npublic static class Program\n{\n    public static void Main() { }\n\n    public static void Other() { }\n}\n");
        File.SetLastWriteTimeUtc(_source, DateTime.UtcNow.AddMinutes(1));
        _secondBuild = new();
        _second = TaskFor(_secondBuild);
    }

    void Because() => _second.Execute();

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

    [Fact] void should_still_hand_back_the_documents() => _second.Documents.Length.ShouldEqual(2);

    [Fact] void should_not_reuse_what_it_embedded_before() =>
        _secondBuild.Messages.Exists(_ => _.Message!.Contains("are reused", StringComparison.Ordinal)).ShouldBeFalse();

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
