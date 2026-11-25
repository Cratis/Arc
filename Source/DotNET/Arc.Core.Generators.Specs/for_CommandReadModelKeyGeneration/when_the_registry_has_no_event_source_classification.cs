// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_the_registry_has_no_event_source_classification : Specification
{
    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics, _) = CommandKeyCompilation.Compile(
        "LegacyRegistry",
        "[Cratis.Arc.Commands.ModelBound.Command] public record Change(string Key);",
        classifyEventSourceKeys: false);

    [Fact] void should_still_generate_the_command_registration() => _source.ShouldContain("RegisterCommand(typeof(global::Change)");
    [Fact] void should_not_reference_the_unavailable_member() => _source.ShouldNotContain("IsEventSourceKey");
    [Fact] void should_compile_without_diagnostics() => _diagnostics.ShouldBeEmpty();
}
