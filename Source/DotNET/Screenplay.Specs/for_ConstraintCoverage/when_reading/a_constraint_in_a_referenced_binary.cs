// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Constraints;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ConstraintCoverage.when_reading;

public class a_constraint_in_a_referenced_binary : Specification
{
    ConstraintCoverage _coverage;
    INamedTypeSymbol _event;
    Compilation _compilation;

    void Establish()
    {
        const string Source = """
            using Cratis.Chronicle.Events.Constraints;
            namespace Rules;
            public class Rule : IConstraint
            {
                public void Define(IConstraintBuilder builder) => builder.Unique<E>();
            }
            public record E(string Name);
            """;
        var package = Analyzed.Package("Rules", Source);
        _compilation = Analyzed.Compile([package], (Analyzed.SlicePath, "namespace Library; public record E(string Name);"));
        _event = _compilation.GetTypeByMetadataName("Library.E")!;
    }

    void Because() => _coverage = ConstraintCoverage.Read([_event], new SemanticModels([_compilation]));

    [Fact] void should_fail_closed_for_batches() => _coverage.UnprovenBatch([_event, _event])!.Message.ShouldContain("constraint 'Rule' cannot be read");
    [Fact] void should_not_withhold_single_events() => _coverage.UnprovenBatch([_event]).ShouldBeNull();
}
