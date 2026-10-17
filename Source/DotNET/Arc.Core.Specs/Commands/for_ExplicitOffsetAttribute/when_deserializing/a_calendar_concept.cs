// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class a_calendar_concept : given.date_inputs
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => JsonSerializer.Deserialize<CalendarConceptInput>("""{"from":"2026-10-16"}""", Options));

    [Fact] void should_refuse_the_configuration() => _exception.ShouldBeOfExactType<ExplicitOffsetRequiresDateTimeOrDateTimeOffset>();
    [Fact] void should_explain_why() => _exception.Message.ShouldContain("DateOnly have no offset");
}
