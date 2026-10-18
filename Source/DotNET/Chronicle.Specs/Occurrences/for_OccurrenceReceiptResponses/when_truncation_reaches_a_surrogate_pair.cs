// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceReceiptResponses;

public class when_truncation_reaches_a_surrogate_pair : Specification
{
    string[] _names;
    string _reason;

    void Because()
    {
        var crossing = new string('a', 244) + "\U0001F600" + new string('z', 20);
        JsonNode[] responses = [OccurrenceReceiptResponses.Appended(crossing, 1), OccurrenceReceiptResponses.NoEvents(crossing), OccurrenceReceiptResponses.Refused(crossing, [crossing])];
        _names = responses.Select(_ => _["command"]!.GetValue<string>()).ToArray();
        _reason = responses[2]["reasons"]![0]!.GetValue<string>();
    }

    [Fact] void should_back_off_before_the_high_surrogate_in_each_command_name() => _names.ShouldEachConformTo(name => name == new string('a', 244) + "[truncated]");
    [Fact] void should_back_off_before_the_high_surrogate_in_a_refusal_reason() => _reason.ShouldEqual(new string('a', 244) + "[truncated]");
}
