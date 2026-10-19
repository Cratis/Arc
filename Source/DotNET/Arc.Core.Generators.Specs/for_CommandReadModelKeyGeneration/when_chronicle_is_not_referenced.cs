// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_chronicle_is_not_referenced : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        [Command] public record Change(string Name) { public void Handle() { } }
        """;

    bool _emitted;

    void Because()
    {
        var (_, _, output) = ModelGraphWalkerCompilation.Compile("KeysWithoutChronicle", Models, true);
        _emitted = output.SyntaxTrees.Any(tree => tree.FilePath.EndsWith("CommandReadModelKeys.g.cs", StringComparison.Ordinal));
    }

    [Fact] void should_not_emit_anything() => _emitted.ShouldBeFalse();
}
