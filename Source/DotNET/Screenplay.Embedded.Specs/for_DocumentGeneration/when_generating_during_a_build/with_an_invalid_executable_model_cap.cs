// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Utilities;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_during_a_build;

public class with_an_invalid_executable_model_cap : Specification
{
    given.a_build _build;
    GenerateEmbeddedScreenplayDocuments _task;
    bool _result;

    void Establish()
    {
        _build = new();
        _task = new()
        {
            BuildEngine = _build,
            AssemblyName = "Library",
            MaximumExecutableModelVersion = "6",
            Sources = [new TaskItem(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs"))],
            OutputPath = Path.Combine(Path.GetTempPath(), $"cratis-screenplay-invalid-cap-{Guid.NewGuid():N}")
        };
    }

    void Because() => _result = _task.Execute();

    [Fact] void should_fail() => _result.ShouldBeFalse();
    [Fact] void should_report_the_cap_before_reading_the_missing_source() => _build.Errors.Single().Message.ShouldContain("CratisEmbeddedScreenplayMaximumExecutableModelVersion");
    [Fact] void should_explain_the_canonical_form() => _build.Errors.Single().Message.ShouldContain("canonical major.minor form (for example '6.0')");
    [Fact] void should_name_the_invalid_value() => _build.Errors.Single().Message.ShouldContain("received '6'");
    [Fact] void should_produce_no_resources() => _task.Documents.ShouldBeEmpty();
    [Fact] void should_write_nothing() => Directory.Exists(_task.OutputPath).ShouldBeFalse();
}
