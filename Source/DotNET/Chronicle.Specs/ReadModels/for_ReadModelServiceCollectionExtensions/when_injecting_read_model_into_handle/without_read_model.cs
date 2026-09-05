// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_injecting_read_model_into_handle;

public class without_read_model : given.nullable_read_model_injection
{
    class Handler
    {
        public void Handle(TestReadModel? readModel) { }
    }

    CommandHandlerArgumentResolver _resolver;
    ICommandHandler _handler;
    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        _resolver = new(new CommandProvideInvoker());
        _handler = Substitute.For<ICommandHandler>();
        _handler.Parameters.Returns(typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters());
    }

    async Task Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(TestCommand), new TestCommand(), [], new());
        _result = await _resolver.Resolve(_handler, context, _serviceProvider, allowedSeverity: null);
    }

    [Fact] void should_inject_null() => _result.Arguments[0].ShouldBeNull();
}
