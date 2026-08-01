// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A key made of several properties is the hardest thing to recover from a fluent projection, because the type it
/// identifies comes from the type argument while its parts come from a nested chain. A key that identified a read
/// model by fewer properties than it really does would be worse than no key at all.
/// </summary>
public class a_projection_keyed_on_several_properties : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;

        namespace Library.Ordering.Tracking;

        [EventType]
        public record OrderPlaced(string CustomerId, int OrderNumber, string Status);

        public record OrderKey(string CustomerId, int Number);

        [ReadModel]
        public record Order
        {
            public string Status { get; init; } = string.Empty;
        }

        public class OrderProjection : IProjectionFor<Order>
        {
            public void Define(IProjectionBuilderFor<Order> builder) => builder
                .NoAutoMap()
                .From<OrderPlaced>(_ => _
                    .UsingCompositeKey<OrderKey>(k => k
                        .Set(m => m.CustomerId).To(e => e.CustomerId)
                        .Set(m => m.Number).To(e => e.OrderNumber))
                    .Set(m => m.Status).To(e => e.Status));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    Model.ProjectionFromModel From => _analysis.Slice().Projection!.Scope.From.Single();

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_turn_automatic_mapping_off() => _analysis.Slice().Projection!.AutoMap.ShouldEqual(Model.ProjectionAutoMapMode.Disabled);
    [Fact] void should_name_the_type_the_key_identifies() => From.Key!.Contains("$composite(OrderKey,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_carry_every_part_of_the_key() => From.Key!.Contains("CustomerId=customerId", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_carry_the_second_part_too() => From.Key!.Contains("Number=orderNumber", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_map_the_ordinary_properties() => From.Properties["Status"].ShouldEqual("status");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
