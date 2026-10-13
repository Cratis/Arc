// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration.when_generating_command_response;

public class with_a_scalar : Specification
{
    string _generatedCode;

    void Because()
    {
        var descriptor = typeof(SummarizeProject).GetMethod(nameof(SummarizeProject.Handle))!
            .ToCommandDescriptor(nameof(SummarizeProject), [], [], "/api/project", "/output", 5, validationRules: []);
        _generatedCode = TemplateTypes.Command(descriptor);
    }

    [Fact] void should_keep_the_scalar_command_response() => _generatedCode.ShouldContain("Command<ISummarizeProject, ProjectSummary>");
    [Fact] void should_keep_the_scalar_hook_response() => _generatedCode.ShouldContain("useCommand<SummarizeProject, ISummarizeProject, ProjectSummary>");
    [Fact] void should_deserialize_a_single_project() => _generatedCode.ShouldContain("super(ProjectSummary, false);");
}
