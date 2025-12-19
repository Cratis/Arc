// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation;

public class when_generating_command_without_documentation : Specification
{
    CommandDescriptor _descriptor = null!;
    string _generatedCode = null!;

    void Establish()
    {
        // Create a command type without documentation
        var commandType = typeof(CommandWithoutDocumentation);
        _descriptor = commandType.GetTypeInfo().ToCommandDescriptor(
            "/output",
            segmentsToSkip: 3,
            skipCommandNameInRoute: false,
            apiPrefix: "api");
    }

    void Because() => _generatedCode = TemplateTypes.Command(_descriptor);

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_not_contain_jsdoc_for_command() => _generatedCode.ShouldNotContain("/**\n * \n */\nexport class CommandWithoutDocumentation");
}

[Cratis.Arc.Commands.ModelBound.Command]
public class CommandWithoutDocumentation
{
    public string Name { get; set; } = string.Empty;

    public void Handle()
    {
    }
}
