// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

/// <summary>
/// The generated files are compiler input, so what is in that directory is what the assembly embeds. A document
/// from an earlier build describing a feature that has since been renamed would be embedded alongside the real
/// ones, and a file rewritten with content identical to what it already held would make the compiler redo work
/// nothing changed for.
/// </summary>
public class and_an_earlier_build_left_files_behind : Specification, IDisposable
{
    const string AssemblyName = "Library";

    string _directory;
    string _output;
    string _stale;
    string _unrelated;
    DateTime _writtenAt;
    given.a_build _build;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-screenplay-{Guid.NewGuid():N}");
        _output = Path.Combine(_directory, "generated");
        Directory.CreateDirectory(_output);

        var source = Path.Combine(_directory, "Program.cs");
        File.WriteAllText(source, "namespace Library;\n");

        _stale = Path.Combine(_output, "Cratis.Arc.Screenplay.Embedded.documents.Library.Gone.play");
        File.WriteAllText(_stale, "domain Gone");
        _unrelated = Path.Combine(_output, "manual.play");
        File.WriteAllText(_unrelated, "domain Manual");

        _build = new();
        _task = new()
        {
            BuildEngine = _build,
            AssemblyName = AssemblyName,
            RootNamespace = AssemblyName,
            OutputPath = _output,
            Sources = [new TaskItem(source)],
            References = [.. Platform().Select(_ => (ITaskItem)new TaskItem(_))]
        };

        _task.Execute();
        _writtenAt = File.GetLastWriteTimeUtc(_task.Documents[0].ItemSpec);
    }

    void Because() => _result = _task.Execute();

    static IEnumerable<string> Platform() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

    [Fact] void should_succeed_again() => _result.ShouldBeTrue();

    [Fact] void should_remove_the_document_nothing_describes_any_longer() => File.Exists(_stale).ShouldBeFalse();
    [Fact] void should_not_delete_a_file_it_did_not_generate() => File.Exists(_unrelated).ShouldBeTrue();

    [Fact] void should_leave_a_file_whose_content_did_not_change_alone() =>
        File.GetLastWriteTimeUtc(_task.Documents[0].ItemSpec).ShouldEqual(_writtenAt);

    [Fact] void should_hand_back_the_same_files_it_did_before() =>
        _task.Documents.Length.ShouldEqual(2);

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
