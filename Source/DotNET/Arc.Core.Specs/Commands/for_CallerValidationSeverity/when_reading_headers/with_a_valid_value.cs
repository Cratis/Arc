// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_reading_headers;

public class with_a_valid_value : Specification
{
    ValidationResultSeverity?[] _results;
    void Because() => _results = Enumerable.Range(0, 4).Select(value => CallerValidationSeverity.FromHeaders(new Dictionary<string, string> { ["x-allowed-SEVERITY"] = value.ToString() })).ToArray();

    [Fact] void should_accept_every_wire_value_case_insensitively() => _results.ShouldEqual([ValidationResultSeverity.Unknown, ValidationResultSeverity.Information, ValidationResultSeverity.Warning, ValidationResultSeverity.Error]);
}
