// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class without_a_mapper : given.a_throwing_command_with_exception_mapping
{
    void Establish() => _serviceProvider.GetService(typeof(IInstancesOf<ICommandExceptionMapper>)).Returns(new KnownInstancesOf<ICommandExceptionMapper>([]));
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_keep_the_default_failure() => _result.ExceptionMessages.ShouldContainOnly(_exception.Message);
    [Fact] void should_keep_the_default_validation_result() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_remain_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
