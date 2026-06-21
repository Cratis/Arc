// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator;

/// <summary>
/// Reproduces the meta-package double-load scenario at the generated-output level: when the Arc generator assembly
/// is loaded from more than one analyzer path in a single compilation, the unconditional marker type is emitted
/// twice. Declaring it <c>partial</c> must let the duplicate declarations merge instead of failing with CS0101.
/// </summary>
public class when_the_marker_is_emitted_more_than_once_in_a_compilation : Specification
{
    string _markerSource;
    ImmutableArray<Diagnostic> _diagnostics;

    void Establish()
    {
        var result = GeneratorTestHelper.RunGenerator(string.Empty);
        _markerSource = GeneratorTestHelper.GetGeneratedSourceByHintName(result, "CratisArcGeneratedMarker.g.cs");
    }

    void Because() => _diagnostics = GeneratorTestHelper.Compile(_markerSource, _markerSource);

    [Fact] void should_declare_the_marker_as_partial() => _markerSource.ShouldContain("partial class GeneratedMarker");
    [Fact] void should_not_produce_a_duplicate_definition_error() => _diagnostics.Any(diagnostic => diagnostic.Id == "CS0101").ShouldBeFalse();
    [Fact] void should_compile_without_errors() => _diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
}
