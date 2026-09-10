// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_legacy_object_response_uses_an_unclassified_scope : given.an_operation_pipeline
{
    void Establish() => _scopes = [_executionScope];
    async Task Because() => _result = await Run(new Legacy());

    [Fact] void should_preserve_legacy_execution() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_legacy_response_shape() => _result.ResponseValue.ShouldEqual("ordinary");
    [Fact] void should_not_classify_object_as_operation_participation() => _result.Recovery.ShouldBeNull();
    [Fact] void should_begin_the_existing_scope() => _executionScope.Received(1).Begin(Arg.Any<CommandContext>());
    [Fact] void should_complete_the_existing_scope() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());

    public record Legacy
    {
        public object Handle() => "ordinary";
    }
}
