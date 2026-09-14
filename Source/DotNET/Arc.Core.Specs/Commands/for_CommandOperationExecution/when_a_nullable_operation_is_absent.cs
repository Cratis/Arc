// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_nullable_operation_is_absent : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Absent());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_enter_no_operations() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_not_produce_a_client_response() => _result.ResponseValue.ShouldBeNull();

    public record Absent
    {
        public given.NamedOperation? Handle() => null;
    }
}
