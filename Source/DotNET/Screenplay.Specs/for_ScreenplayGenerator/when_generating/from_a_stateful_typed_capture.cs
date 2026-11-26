// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_stateful_typed_capture : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_recover_a_native_capture_that_binds_and_round_trips(bool keyIsEventSource, bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, typed_capture_source.Files(keyIsEventSource));

        Result.Source.ShouldContain("slice Translate ReceiveOrders");
        Result.Source.ShouldContain("capture OrderIntake");
        Result.Source.ShouldContain("from OrderPlaced");
        Result.Source.ShouldContain(keyIsEventSource ? "key orderId is event source" : "key orderId");
        Result.Source.ShouldContain("append OrderReceived");
        Result.Source.ShouldContain("orderId = $.orderId");
        Result.Source.ShouldContain("public event OrderPlaced from \"storefront\"");
        Result.Source.ShouldNotContain("reaction");
        Result.Source.ShouldNotContain("file Sales/Orders/ReceiveOrders/OrderIntake.cs");
        Result.Source.ShouldContain("orderId OrderId key");
        Result.Source.ShouldContain("projection OrderStateEntry");
        Result.Source.IndexOf("slice Translate ReceiveOrders", StringComparison.Ordinal)
            .ShouldBeLessThan(Result.Source.IndexOf("slice StateView OrderState", StringComparison.Ordinal));
        var codes = new[] { "PLAY0268", "PLAY0516" };
        Result.Diagnostics.Where(diagnostic => codes.Contains(diagnostic.Code)).Select(diagnostic => diagnostic.Code + " " + diagnostic.Message).ShouldBeEmpty();
        Bound.Diagnostics.Where(diagnostic => codes.Contains(diagnostic.Code)).Select(diagnostic => diagnostic.Code + " " + diagnostic.Message).ShouldBeEmpty();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0270" && (diagnostic.Message.Contains("apture", StringComparison.Ordinal) || diagnostic.Message.Contains("eaction", StringComparison.Ordinal))).ShouldBeEmpty();
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        AssertDocument();

        var slices = Bound.Value!.Model.Application.Modules.Single().Features.SelectMany(Flatten).ToList();
        var capture = slices.Single(slice => slice.Name == "ReceiveOrders").Captures.Single();
        capture.Key.ShouldEqual("orderId");
        capture.EventsSource!.KeyIsEventSource.ShouldEqual(keyIsEventSource);
        capture.Appends.Length.ShouldEqual(1);
        Bound.Value.Model.SemanticVersion.Major.ShouldEqual(keyIsEventSource ? 33u : 32u);
    }

    static IEnumerable<SemanticSlice> Flatten(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Flatten));
}
