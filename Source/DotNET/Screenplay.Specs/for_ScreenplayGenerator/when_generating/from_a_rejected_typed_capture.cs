// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_rejected_typed_capture : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_keep_the_whole_reaction_when_the_slice_also_projects_a_local_event(bool keyIsEventSource, bool authoring)
    {
        var sources = typed_capture_source.Files(keyIsEventSource)
            .Select(source => source.Path.StartsWith(typed_capture_source.StatePath, StringComparison.Ordinal)
                ? (source.Path.Replace(typed_capture_source.StatePath, typed_capture_source.ReceivePath, StringComparison.Ordinal), source.Text.Replace("namespace Library.Sales.Orders.OrderState;", "namespace Library.Sales.Orders.ReceiveOrders;", StringComparison.Ordinal))
                : source).ToArray();
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, sources);

        AssertDocument();
        AssertFallback();
        Result.Source.ShouldContain("projection");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_keep_the_whole_reaction_when_a_constructor_parameter_is_not_its_event_property(bool keyIsEventSource, bool authoring)
    {
        var sources = typed_capture_source.Files(keyIsEventSource)
            .Select(source => source.Path.EndsWith("/OrderReceived.cs", StringComparison.Ordinal)
                ? (source.Path, source.Text.Replace("public record OrderReceived(global::Library.Sales.Orders.OrderId OrderId);", ExplicitConstructor, StringComparison.Ordinal))
                : source).ToArray();
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, sources);

        AssertDocument();
        AssertFallback();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_keep_the_whole_reaction_when_the_source_event_is_in_another_slice(bool keyIsEventSource, bool authoring)
    {
        var sources = typed_capture_source.Files(keyIsEventSource,
                reactor => reactor.Replace("global::Library.Sales.Orders.ReceiveOrders.OrderPlaced", "global::Library.Sales.Orders.ForeignOrders.OrderPlaced", StringComparison.Ordinal))
            .Select(source => source.Path.EndsWith("/OrderPlaced.cs", StringComparison.Ordinal)
                ? ("Library/Sales/Orders/ForeignOrders/OrderPlaced.cs", source.Text.Replace("namespace Library.Sales.Orders.ReceiveOrders;", "namespace Library.Sales.Orders.ForeignOrders;", StringComparison.Ordinal))
                : source).ToArray();
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, sources);

        AssertDocument();
        AssertFallback();
    }

    void AssertFallback()
    {
        Result.Source.ShouldNotContain("capture OrderIntake");
        Result.Source.ShouldContain("reaction");
        Result.Source.ShouldContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
    }

    const string ExplicitConstructor = """
        public record OrderReceived
        {
            public global::Library.Sales.Orders.OrderId Id { get; init; }
            public OrderReceived(global::Library.Sales.Orders.OrderId orderId) { Id = orderId; }
        }
        """;
}
