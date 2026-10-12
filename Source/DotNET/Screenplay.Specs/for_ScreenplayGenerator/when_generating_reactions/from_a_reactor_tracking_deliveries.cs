// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_reactor_tracking_deliveries : a_returned_event_batch
{
    [Theory]
    [InlineData("public void Welcome(AuthorRegistered @event, ReactorDelivery delivery) { if (!_receipts.Add(delivery.Id)) return; Console.WriteLine(@event.Name); }", false)]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event, ReactorDelivery delivery) => new(@event.Name, \"Welcome\");", true)]
    [InlineData("public void Welcome(AuthorRegistered @event, EventContext context) { var delivery = ReactorDelivery.For(this, context); if (!_receipts.Add(delivery.Id)) return; Console.WriteLine(@event.Name); }", false)]
    [InlineData("public void Welcome(AuthorRegistered @event) { DeliveryId receipt = DeliveryId.NotSet; if (!_receipts.Add(receipt)) return; }", true)]
    public void should_explain_why_receipt_based_redelivery_stays_code(string handler, bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + $$"""
                public class Welcomer : IReactor
                {
                    readonly HashSet<DeliveryId> _receipts = new();
                    {{handler}}
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            (ScenarioPath, Scenario)
        ]);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        var report = Result.Diagnostics.Single(_ => _.Code == "SP0072");
        report.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        report.Location.ShouldEqual("Library.Authors.Welcoming.Welcomer");
        report.Message.ShouldContain("Screenplay#433");
        Result.Diagnostics.Where(_ => _.Code == "SP0071").ShouldBeEmpty();
        ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);
        AssertDocument();
    }
}
