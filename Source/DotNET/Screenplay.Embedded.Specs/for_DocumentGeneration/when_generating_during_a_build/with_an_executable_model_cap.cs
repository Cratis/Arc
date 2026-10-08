// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

public class with_an_executable_model_cap : Specification, IDisposable
{
    string _directory;
    given.a_build _build;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cratis-screenplay-cap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "EchoName.cs");
        File.WriteAllText(source, "using Cratis.Arc.Commands.ModelBound;\nnamespace Library.Authors.Registration;\n[Command] public record EchoName(string Name)\n{\n    public string Handle() => Name;\n}\n");
        _build = new();
        _task = new()
        {
            BuildEngine = _build,
            AssemblyName = "Library",
            MaximumExecutableModelVersion = "6.0",
            OutputPath = Path.Combine(_directory, "generated"),
            Sources = [new TaskItem(source)],
            References = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(path => (ITaskItem)new TaskItem(path))]
        };
    }

    void Because() => _result = _task.Execute();

    [Fact] void should_succeed() => _result.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _build.Errors.ShouldBeEmpty();
    [Fact] void should_keep_the_command_in_every_document() => Sources().All(source => source.Contains("command EchoName", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_withhold_returns_in_every_document() => Sources().All(source => !source.Contains("returns", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_cap() => _build.Messages.Exists(message => message.Message!.Contains("MaximumExecutableModelVersion is capped at ESM v6.0", StringComparison.Ordinal)).ShouldBeTrue();

    IEnumerable<string> Sources() => _task.Documents.Where(document => document.ItemSpec.EndsWith(".play", StringComparison.Ordinal)).Select(document => File.ReadAllText(document.ItemSpec));

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_directory, true);
    }
}
