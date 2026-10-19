// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_chronicle_is_not_referenced : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        public record Composite(string Part);
        [Command] public record Change(Composite Key) { public void Handle() { } }
        """;

    bool _hasCompositeOutput;

    void Because()
    {
        var (_, _, output) = ModelGraphWalkerCompilation.Compile("WithoutChronicleCoverage", Models, true);
        _hasCompositeOutput = output.SyntaxTrees.Any(tree => tree.FilePath.EndsWith("ProtectedReadModelKeyAccessors.g.cs", StringComparison.Ordinal));
    }

    [Fact] void should_not_emit_a_chronicle_dependency() => _hasCompositeOutput.ShouldBeFalse();
}
