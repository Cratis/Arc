// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class and_the_property_type_is_unsupported : given.date_inputs
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => JsonSerializer.Deserialize<UnsupportedInput>("""{"from":"invalid"}""", Options));

    [Fact] void should_report_a_configuration_failure() => _exception.ShouldBeOfExactType<ExplicitOffsetRequiresDateTimeOrDateTimeOffset>();
}
