// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications;

public class with_an_issued_enum_event : given.a_recovered_command
{
    void Establish()
    {
        Scenario = Scenario.Replace("when_draft_status", "when_issued_status", StringComparison.Ordinal)
            .Replace(StatusField, string.Empty, StringComparison.Ordinal)
            .Replace("RecordStatus", "Issue", StringComparison.Ordinal)
            .Replace("new Issue(_id, _status)", "new Issue(_id)", StringComparison.Ordinal)
            .Replace("@event.Status == _status", "@event.Status == InvoiceStatus.Issued", StringComparison.Ordinal);
        Name = "IssuedStatus";
        Status = "issued";
        Command = "Issue";
    }

    void Because() => GenerateScenario();
    [Fact] void should_recover_the_exact_captured_scenario() => AssertRecovered();
}
