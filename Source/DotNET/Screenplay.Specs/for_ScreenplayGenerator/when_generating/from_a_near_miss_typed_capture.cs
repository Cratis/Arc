// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_near_miss_typed_capture : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_keep_every_part_of_a_reactor_that_is_not_exactly_the_capture_template(bool keyIsEventSource, bool authoring)
    {
        foreach (var (before, after) in _mutations)
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
                typed_capture_source.Files(keyIsEventSource, source =>
                {
                    source.ShouldContain(before);
                    return source.Replace(before, after, StringComparison.Ordinal);
                }));

            Result.Source.ShouldNotContain("capture OrderIntake");
            Result.Source.ShouldContain("reaction");
            Result.Source.ShouldContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
            AssertDocument();
        }
    }

    static readonly (string Before, string After)[] _mutations =
    [
        ("if (previous is not null && SameState(previous, current))", "if (previous is not null && !SameState(previous, current))"),
        (", global::Cratis.Chronicle.Events.EventContext context, global::Cratis.Chronicle.Reactors.ReactorDelivery delivery)\n    {", ")\n    {\n        global::Cratis.Chronicle.Events.EventContext context = default!; global::Cratis.Chronicle.Reactors.ReactorDelivery delivery = default!;"),
        ("EventStoreAttribute(\"storefront\")", "EventStoreAttribute(\"another-store\")"),
        ("var effects = new global::System.Collections.Generic.List<object>();", "var effects = new global::System.Collections.Generic.List<object>(); global::System.GC.KeepAlive(@event);"),
        ("\"orderId\", required: true)", "\"orderId\", required: false)"),
        ("    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.Dictionary<string, object?>?> Previous", "    public void AdditionalHandler() { }\n\n    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.Dictionary<string, object?>?> Previous")
    ];
}
