// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration.when_generating_command_response;

public class with_a_typed_collection : Specification
{
    string _generatedCode;

    void Because()
    {
        var descriptor = typeof(SummarizeProjects).GetMethod(nameof(SummarizeProjects.Handle))!
            .ToCommandDescriptor(nameof(SummarizeProjects), [], [], "/api/projects", "/output", 5, validationRules: []);
        _generatedCode = TemplateTypes.Command(descriptor);
    }

    [Fact] void should_type_the_command_response_as_an_array() => _generatedCode.ShouldContain("Command<ISummarizeProjects, ProjectSummary[]>");
    [Fact] void should_type_the_hook_response_as_an_array() => _generatedCode.ShouldContain("useCommand<SummarizeProjects, ISummarizeProjects, ProjectSummary[]>");
    [Fact] void should_deserialize_each_project() => _generatedCode.ShouldContain("super(ProjectSummary, true);");
    [Fact] void should_import_the_element_type() => _generatedCode.ShouldContain("import { ProjectSummary } from ");
    [Fact] void should_not_import_an_array_type() => _generatedCode.ShouldNotContain("import { ProjectSummary[] }");
}
