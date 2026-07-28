// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A nested object carries no key of its own - there is only ever one of it - and the property declaring it says
/// nothing beyond where it lives, so everything about it has to be read from the type it holds.
/// </summary>
public class a_model_bound_projection_with_a_nested_object : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Lending.Delivery;

        [EventType]
        public record LoanRequested(string Reference);

        [EventType]
        public record LoanShipped(string Carrier, string TrackingNumber);

        [FromEvent<LoanShipped>]
        public record Shipment(
            [SetFrom<LoanShipped>(nameof(LoanShipped.Carrier))] string Carrier,
            [SetFrom<LoanShipped>(nameof(LoanShipped.TrackingNumber))] string TrackingNumber);

        [ReadModel]
        [FromEvent<LoanRequested>]
        public record Loan(
            string Reference,
            [Nested] Shipment? Shipping);
        """;

    ApplicationModelAnalysis _analysis;
    ProjectionChildScopeModel _nested;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _nested = _analysis.Slice().Projection!.Scope.Nested.Single();
    }

    ProjectionFromModel From => _nested.Scope.From.Single();

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_hold_the_object_in_the_property_declaring_it() => _nested.Property.ShouldEqual("Shipping");
    [Fact] void should_identify_it_by_nothing_at_all() => _nested.IdentifiedBy.ShouldEqual(string.Empty);
    [Fact] void should_leave_automatic_mapping_to_the_enclosing_scope() => _nested.AutoMap.ShouldEqual(ProjectionAutoMapMode.Inherit);
    [Fact] void should_observe_the_event_its_type_names() => From.EventTypes.ShouldContainOnly(["LoanShipped"]);
    [Fact] void should_map_what_its_type_declares() => From.Properties["Carrier"].ShouldEqual("carrier");
    [Fact] void should_map_every_property_its_type_declares() => From.Properties["TrackingNumber"].ShouldEqual("trackingNumber");
    [Fact] void should_declare_no_children_alongside_it() => _analysis.Slice().Projection!.Scope.Children.ShouldBeEmpty();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
