// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_a_composite_key_is_partial : Specification
{
    const string Models = """
        namespace Keys
        {
            public partial record Composite(string Part);
            [Cratis.Arc.Commands.ModelBound.Command]
            public record Change([Cratis.Chronicle.Keys.Key] Composite Key) { public void Handle() { } }
        }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics) = CompositeKeyCompilation.Compile("PartialCompositeCoverage", Models);

    [Fact] void should_not_claim_complete_coverage() => _source.ShouldNotContain(".Register<global::Keys.Composite>(");
    [Fact] void should_still_record_the_declared_type_for_startup() => _source.ShouldContain("typeof(global::Keys.Composite)");
    [Fact] void should_explain_the_registration_requirement() => _diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ARC0030" && diagnostic.Severity == DiagnosticSeverity.Info);
    [Fact] void should_not_prevent_separate_manual_registration() => _diagnostics.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty();
}
