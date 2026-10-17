// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceReceiptResponses;

public class when_describing_a_validation_refusal : Specification
{
    string _json;

    void Because()
    {
        var members = Enumerable.Range(0, 5000).Select(_ => $"Member{_}").ToArray();
        var result = new CommandResult { CorrelationId = CorrelationId.New(), ValidationResults = [ValidationResult.Error("jane@example.com is taken", members)] };
        var refusal = result.GetRefusal()!;
        _json = OccurrenceReceiptResponses.Refused("Register", OccurrenceReceiptResponses.ReasonsFor(result, refusal)).ToJsonString();
    }

    [Fact] void should_not_contain_the_validation_message() => _json.ShouldNotContain("jane@example.com");
    [Fact] void should_name_the_command() => _json.ShouldContain("Register");
    [Fact] void should_stay_within_the_receipt_limit() => (Encoding.UTF8.GetByteCount(_json) <= 64 * 1024).ShouldBeTrue();
    [Fact] void should_cap_the_listed_members() => _json.ShouldNotContain("Member25");
}
