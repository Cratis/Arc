// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration.when_generating_command_response;

public class without_a_response : Specification
{
    string _generatedCode;

    void Because()
    {
        var descriptor = typeof(SimpleCommand).GetMethod(nameof(SimpleCommand.Handle))!
            .ToCommandDescriptor(nameof(SimpleCommand), [], [], "/api/simple", "/output", 5, validationRules: []);
        _generatedCode = TemplateTypes.Command(descriptor);
    }

    [Fact] void should_omit_the_command_response_generic() => _generatedCode.ShouldContain("Command<ISimpleCommand>");
    [Fact] void should_omit_the_hook_response_generic() => _generatedCode.ShouldContain("useCommand<SimpleCommand, ISimpleCommand>");
    [Fact] void should_keep_the_default_constructor() => _generatedCode.ShouldContain("super(Object, false);");
}
