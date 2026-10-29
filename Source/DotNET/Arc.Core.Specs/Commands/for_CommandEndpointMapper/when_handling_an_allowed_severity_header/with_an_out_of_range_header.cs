// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_handling_an_allowed_severity_header;

public class with_an_out_of_range_header : given.a_custom_pipeline
{
    void Establish() => _header = "999";
    Task Because() => HandleEndpoints();

    [Fact] void should_ignore_the_header_for_both_endpoints() => _severities.ShouldEqual(new ValidationResultSeverity?[] { null, null });
}
