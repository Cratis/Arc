// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

public class with_sparse_enum_numbers_below_v12 : Specification, IDisposable
{
    string _directory;
    given.a_build _build;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _result;

    void Establish()
    {
        _directory = Path.Join(Path.GetTempPath(), $"cratis-screenplay-enum-cap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        var source = Path.Join(_directory, "PlaceOrder.cs");
        File.WriteAllText(source, "using Cratis.Arc.Commands.ModelBound;\nnamespace Library.Orders.Placing;\npublic enum Status { Draft = 1, Issued = 4 }\n[Command] public record PlaceOrder(Status Status) { public void Handle() {} }\n");
        _build = new();
        _task = new()
        {
            BuildEngine = _build,
            AssemblyName = "Library",
            MaximumExecutableModelVersion = "11.0",
            OutputPath = Path.Join(_directory, "generated"),
            Sources = [new TaskItem(source)],
            References = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(path => (ITaskItem)new TaskItem(path))]
        };
    }

    void Because() => _result = _task.Execute();

    [Fact] void should_succeed() => _result.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _build.Errors.ShouldBeEmpty();
    [Fact] void should_report_the_lost_numbers_as_sp0068() => _build.Warnings.Exists(warning => warning.Code == "SP0068" && warning.Message!.Contains("cap v11.0", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_bare_names_in_every_document() => _task.Documents.Where(document => document.ItemSpec.EndsWith(".play", StringComparison.Ordinal)).Select(document => File.ReadAllText(document.ItemSpec)).All(source => source.Contains("  draft\n  issued\n", StringComparison.Ordinal)).ShouldBeTrue();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_directory, true);
    }
}
