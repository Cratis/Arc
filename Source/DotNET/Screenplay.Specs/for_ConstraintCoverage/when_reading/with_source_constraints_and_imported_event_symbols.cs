// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Constraints;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ConstraintCoverage.when_reading;

public class with_source_constraints_and_imported_event_symbols
{
    [Theory]
    [InlineData("builder.Unique<B>();", "can be rejected")]
    [InlineData("builder.ClosesStreamOn<A>();", "closes the stream")]
    public void should_match_metadata_events_to_analyzed_source_coverage(string declaration, string reason)
    {
        var source = $$"""
            using Cratis.Chronicle.Events.Constraints;
            namespace Contracts;
            public record A(string Name);
            public record B(string Name);
            public class Rule : IConstraint
            {
                public void Define(IConstraintBuilder builder) => {{declaration}}
            }
            """;
        var contracts = Analyzed.Project("Contracts", [], ("Library/Contracts.cs", source));
        using var stream = new MemoryStream();
        contracts.Emit(stream, cancellationToken: global::Xunit.TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        var reactor = Analyzed.Project("Reactors", [MetadataReference.CreateFromImage(stream.ToArray())], ("Library/Reactors.cs", "namespace Reactors; public class Reactor;"));
        var coverage = ConstraintCoverage.Read([.. ArtifactCatalog.From(contracts).Types, .. ArtifactCatalog.From(reactor).Types], new SemanticModels([contracts, reactor]));

        var gap = coverage.UnprovenBatch([reactor.GetTypeByMetadataName("Contracts.A")!, reactor.GetTypeByMetadataName("Contracts.B")!]);
        gap!.Message.ShouldContain(reason);
        gap.Kind.ShouldEqual(declaration.Contains("ClosesStreamOn", StringComparison.Ordinal) ? ReactionBatchGapKind.Closing : ReactionBatchGapKind.Rejection);
    }
}
