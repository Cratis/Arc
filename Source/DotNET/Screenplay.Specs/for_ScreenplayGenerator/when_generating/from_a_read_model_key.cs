// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_read_model_key : a_generated_document
{
    const string Keyed = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Sales.Orders.OrderState;

        [EventType]
        public record OrderReceived(string OrderId);

        [ReadModel]
        [FromEvent<OrderReceived>]
        public record OrderStateEntry([Key, SetFromContext<OrderReceived>(nameof(EventContext.EventSourceId))] string OrderId, string Note);
        """;

    [Fact]
    void should_emit_key_only_on_the_property_declaring_it() =>
        GenerateSource(Keyed).ShouldContain("orderId String key");

    [Fact]
    void should_leave_a_property_without_the_attribute_unkeyed() =>
        GenerateSource(Keyed.Replace("[Key, ", "[", StringComparison.Ordinal)).ShouldNotContain("orderId String key");

    string GenerateSource(string source)
    {
        Generate(("Library/Sales/Orders/OrderState/OrderStateEntry.cs", source));
        return Result.Source;
    }
}
