// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_a_command_with_a_provide_method;

public class when_resolving_and_handling_with_cancellation_token : Specification
{
    record ProvidedValue(string Value);

    record Command
    {
        public CancellationToken ProvideCancellationToken;
        public CancellationToken HandleCancellationToken;

        public ProvidedValue Provide(CancellationToken cancellationToken)
        {
            ProvideCancellationToken = cancellationToken;
            return new("provided");
        }

        public string Handle(ProvidedValue provided, CancellationToken cancellationToken)
        {
            HandleCancellationToken = cancellationToken;
            return provided.Value;
        }
    }

    readonly CancellationTokenSource _cancellationTokenSource = new();
    IServiceProvider _serviceProvider;
    ICommandHandler _handler;
    CommandHandlerArgumentResolver _resolver;
    Command _command;
    object _result;

    void Establish()
    {
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        _resolver = new CommandHandlerArgumentResolver(new CommandProvideInvoker());
        _handler = new ModelBoundCommandHandler(typeof(Command), typeof(Command).GetMethod(nameof(Command.Handle))!);
        _command = new();
    }

    async Task Because()
    {
        var context = new CommandContext(
            CorrelationId.New(),
            typeof(Command),
            _command,
            [],
            new(),
            CancellationToken: _cancellationTokenSource.Token);
        var resolution = await _resolver.Resolve(_handler, context, _serviceProvider, allowedSeverity: null);
        context = context with { Dependencies = resolution.Arguments };
        _result = await _handler.Handle(context);
    }

    void Destroy()
    {
        (_serviceProvider as IDisposable)?.Dispose();
        _cancellationTokenSource.Dispose();
    }

    [Fact] void should_produce_the_result_from_handle() => _result.ShouldEqual("provided");
    [Fact] void should_pass_the_cancellation_token_to_provide() => _command.ProvideCancellationToken.ShouldEqual(_cancellationTokenSource.Token);
    [Fact] void should_pass_the_cancellation_token_to_handle() => _command.HandleCancellationToken.ShouldEqual(_cancellationTokenSource.Token);
}
