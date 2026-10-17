// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.Filters.for_DataAnnotationValidationFilter;

public class when_validating_an_event_source_id : Specification
{
    [Theory]
    [InlineData(50, true)]
    [InlineData(0, false)]
    public async Task should_validate_the_underlying_value(int value, bool isValid)
    {
        var command = new WithOrderId(new(value));
        var context = new CommandContext(CorrelationId.New(), typeof(WithOrderId), command, [], new());
        var result = await new DataAnnotationValidationFilter().OnExecution(context);

        result.IsValid.ShouldEqual(isValid);
        if (isValid)
        {
            result.ValidationResults.ShouldBeEmpty();
        }
        else
        {
            result.ValidationResults.Single().Members.ShouldContainOnly("orderId");
        }
    }

    record OrderId(int Value) : EventSourceId<int>(Value);
    record WithOrderId([property: Range(1, 100)] OrderId OrderId);
}
