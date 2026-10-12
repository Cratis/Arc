// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration.when_generating_command_response;

public class with_an_object_collection : Specification
{
    string _generatedCode;

    void Because()
    {
        var descriptor = typeof(CollectObjects).GetMethod(nameof(CollectObjects.Handle))!
            .ToCommandDescriptor(nameof(CollectObjects), [], [], "/api/objects", "/output", 5, validationRules: []);
        _generatedCode = TemplateTypes.Command(descriptor);
    }

    [Fact] void should_type_the_command_response_as_an_object_array() => _generatedCode.ShouldContain("Command<ICollectObjects, Record<string, unknown>[]>");
    [Fact] void should_type_the_hook_response_as_an_object_array() => _generatedCode.ShouldContain("useCommand<CollectObjects, ICollectObjects, Record<string, unknown>[]>");
    [Fact] void should_deserialize_an_object_collection() => _generatedCode.ShouldContain("super(Object, true);");
    [Fact] void should_not_import_the_builtin_object_type() => _generatedCode.ShouldNotContain("import { object }");
}
