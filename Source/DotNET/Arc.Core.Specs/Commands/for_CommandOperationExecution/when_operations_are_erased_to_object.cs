// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_operations_are_erased_to_object : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Erased());

    [Fact] void should_reject_the_unsupported_shape() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_start_no_external_work() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_not_expose_the_descriptor() => _result.ResponseValue.ShouldBeNull();

    public record Erased
    {
        public object Handle() => new given.NamedOperation("A");
    }
}
