// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A nested object can be emptied again, which the model a projection is built from carries nowhere. Saying so is
/// what keeps the rest of the nested object trustworthy - the reader that quietly drops one half of a declaration is
/// the reader nobody can tell apart from one that read it wrong.
/// </summary>
public class a_nested_object_emptied_by_an_event : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Lending.Delivery;

        [EventType]
        public record LoanRequested(string Reference);

        [EventType]
        public record LoanShipped(string Carrier);

        [EventType]
        public record LoanShipmentCancelled();

        [FromEvent<LoanShipped>]
        [ClearWith<LoanShipmentCancelled>]
        public record Shipment(
            [SetFrom<LoanShipped>(nameof(LoanShipped.Carrier))] string Carrier);

        [ReadModel]
        [FromEvent<LoanRequested>]
        public record Loan(
            string Reference,
            [Nested] Shipment? Shipping);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_nested_object() => _analysis.Slice().Projection!.Scope.Nested.Single().Property.ShouldEqual("Shipping");
    [Fact] void should_still_recover_what_fills_it_in() => _analysis.Slice().Projection!.Scope.Nested.Single().Scope.From.Single().Properties["Carrier"].ShouldEqual("carrier");
    [Fact] void should_report_what_it_left_out() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableProjectionConstruct);
    [Fact] void should_name_the_event_emptying_it() => _analysis.Diagnostics.Any(_ => _.Message.Contains("LoanShipmentCancelled", StringComparison.Ordinal)).ShouldBeTrue();
}
