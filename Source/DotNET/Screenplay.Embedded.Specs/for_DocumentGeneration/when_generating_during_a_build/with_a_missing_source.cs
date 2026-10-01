// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

public class with_a_missing_source : Specification
{
    given.a_build _engine;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _succeeded;

    void Establish()
    {
        _engine = new();
        _task = new()
        {
            BuildEngine = _engine,
            AssemblyName = "MissingSource",
            Sources = [new TaskItem(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs"))],
            OutputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}")
        };
    }

    void Because() => _succeeded = _task.Execute();

    [Fact] void should_fail_instead_of_analyzing_a_smaller_compilation() => _succeeded.ShouldBeFalse();
    [Fact] void should_report_the_missing_source_as_an_error() => _engine.Errors.ShouldNotBeEmpty();
    [Fact] void should_produce_no_resources() => _task.Documents.ShouldBeEmpty();
    [Fact] void should_write_nothing() => Directory.Exists(_task.OutputPath).ShouldBeFalse();
}
