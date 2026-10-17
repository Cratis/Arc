// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceReceiptResponses;

public class when_bounding_adversarial_receipt_data : Specification
{
    JsonNode[] _responses;
    string _refused;

    void Because()
    {
        var escaped = new string('\u0001', 100_000);
        var reasons = Enumerable.Repeat(escaped, 100_000);
        _responses = [OccurrenceReceiptResponses.Appended(escaped, int.MaxValue), OccurrenceReceiptResponses.NoEvents(escaped), OccurrenceReceiptResponses.Refused(escaped, reasons)];
        _refused = _responses[2].ToJsonString();
    }

    [Fact] void should_bound_every_outcome_including_json_escaping() => _responses.ShouldEachConformTo(response => Encoding.UTF8.GetByteCount(response.ToJsonString()) <= 64 * 1024);
    [Fact] void should_mark_truncated_data() => _responses.ShouldEachConformTo(response => response["command"]!.GetValue<string>().EndsWith("[truncated]", StringComparison.Ordinal));
    [Fact] void should_bound_the_reason_count() => _responses[2]["reasons"]!.AsArray().Count.ShouldEqual(17);
    [Fact] void should_mark_omitted_reasons() => _responses[2]["reasons"]!.AsArray()[^1]!.GetValue<string>().ShouldEqual("[truncated]");
    [Fact] void should_keep_the_outcome() => _refused.ShouldContain("Refused");
}
