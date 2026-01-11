// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation;

public class when_generating_command_with_documentation : Specification
{
    CommandDescriptor _descriptor = null!;
    string _generatedCode = null!;

    void Establish()
    {
        var commandType = typeof(Scenarios.for_ProxyGeneration.SimpleCommand);
        _descriptor = commandType.GetTypeInfo().ToCommandDescriptor(
            "/output",
            segmentsToSkip: 3,
            skipCommandNameInRoute: false,
            apiPrefix: "api",
            [commandType.GetTypeInfo()]);
    }

    void Because() => _generatedCode = TemplateTypes.Command(_descriptor);

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_contain_command_documentation() => _generatedCode.ShouldContain("A simple command for testing proxy generation");
    [Fact] void should_contain_name_property_documentation() => _generatedCode.ShouldContain("Gets or sets the name");
    [Fact] void should_contain_value_property_documentation() => _generatedCode.ShouldContain("Gets or sets the value");
    [Fact] void should_contain_jsdoc_comment_markers() => _generatedCode.ShouldContain("/**");
}
