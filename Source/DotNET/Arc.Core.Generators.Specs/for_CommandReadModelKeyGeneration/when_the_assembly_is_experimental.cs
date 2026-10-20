// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_the_assembly_is_experimental : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        #pragma warning disable CS8305
        [assembly: System.Diagnostics.CodeAnalysis.Experimental("EXP001")]
        [Command] public record Change(string Name) { public void Handle() { } }
        """;

    string _source;

    void Because() => (_source, _, _) = CommandKeyCompilation.Compile("ExperimentalKeys", Models);

    [Fact] void should_not_emit_anything() => _source.ShouldBeEmpty();
}
