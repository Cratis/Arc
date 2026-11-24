// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_mapping_exceptions;

public class before_exception_response_mapping : given.a_throwing_command_with_exception_mapping
{
    IExceptionResponseMapper _responseMapper;

    void Establish()
    {
        _mapper.TryMap(Arg.Any<CommandContext>(), _exception, out Arg.Any<CommandResult>()).Returns(call =>
        {
            call[2] = _mapped;
            return true;
        });
        _responseMapper = Substitute.For<IExceptionResponseMapper>();
        _serviceProvider.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)).Returns(new KnownInstancesOf<IExceptionResponseMapper>([_responseMapper]));
    }

    async Task Because()
    {
        _result = await _commandPipeline.Execute(_command, _serviceProvider);
        ExceptionResponseMapping.Apply(_result, _serviceProvider, NullLogger.Instance);
    }

    [Fact] void should_keep_the_domain_rejection() => _result.ValidationResults.ShouldContainOnly(_mapped.ValidationResults.Single());
    [Fact] void should_not_offer_it_to_the_response_mapper() => _responseMapper.DidNotReceive().TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>());
    [Fact] void should_not_add_a_response_code() => _result.ErrorCode.ShouldBeNull();
}
