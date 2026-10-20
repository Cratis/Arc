// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_a_command_cannot_be_named : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        namespace Unnamed;
        [Command] public partial record Partial(string Name) { public void Handle() { } }
        [Command] public record Named(string Name) { public void Handle() { } }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics, _) = CommandKeyCompilation.Compile("UnnamedCommand", Models);

    [Fact] void should_skip_the_command() => _source.ShouldNotContain("Unnamed.Partial");
    [Fact] void should_report_the_command_as_info() => _diagnostics.Count(diagnostic => diagnostic.Id == "ARC0031" && diagnostic.Severity == DiagnosticSeverity.Info).ShouldEqual(1);
    [Fact] void should_name_the_command_in_the_diagnostic() => _diagnostics.Single(diagnostic => diagnostic.Id == "ARC0031").GetMessage().ShouldContain("Unnamed.Partial");
    [Fact] void should_not_report_anything_else() => _diagnostics.Count(diagnostic => diagnostic.Id != "ARC0031").ShouldEqual(0);
    [Fact] void should_still_register_other_commands() => _source.ShouldContain(".RegisterCommand(typeof(global::Unnamed.Named)");
}
