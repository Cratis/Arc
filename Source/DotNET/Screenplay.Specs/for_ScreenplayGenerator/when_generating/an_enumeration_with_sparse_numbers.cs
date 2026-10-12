// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class an_enumeration_with_sparse_numbers : a_generated_document
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Orders.Placing;
        public enum Status { Draft = 1, Issued = 4 }
        [EventType] public record OrderPlaced(Status Status);
        [Command] public record PlaceOrder([Cratis.Chronicle.Keys.Key] string Id, Status Status)
        {
            public OrderPlaced Handle() => new(Status);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Orders.Placing;
        using Xunit;
        namespace Library.Orders.Placing.when_placing;
        public class and_the_order_is_issued
        {
            readonly CommandScenario<PlaceOrder> _scenario = new();
            async Task Because() => await _scenario.Execute(new PlaceOrder("order", Status.Issued));
            [Fact] Task should_place() => _scenario.EventSequence.ShouldHaveAppendedEvent<OrderPlaced>("order", e => e.Status == Status.Issued);
        }
        """;

    void Because() => Generate((Analyzed.SlicePath, Source), ("Library/Orders/Placing/when_placing/and_the_order_is_issued.cs", Scenario), (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_pin_every_member() => Result.Source.ShouldContain("draft = 1\n  issued = 4");
    [Fact] void should_bind_v12() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_carry_numbers_in_the_executable_model() => Bound.Value!.Model.Application.Concepts.Single().Numbers!.Value.AsEnumerable().ShouldEqual([1, 4]);
    [Fact] void should_keep_scenario_values_name_based() => Result.Source.Split('\n').Count(line => line.Trim() == "status = \"issued\"").ShouldEqual(2);
    [Fact] void should_execute_the_unchanged_scenario() => Run("WhenPlacingAndTheOrderIsIssued").Passed.ShouldBeTrue();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_report_no_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
}
