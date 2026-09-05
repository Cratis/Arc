// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.given;

public class a_command_handler_argument_resolver : Specification
{
    protected ICommandProvideInvoker _provideInvoker;
    protected CommandHandlerArgumentResolver _resolver;
    protected IServiceProvider _serviceProvider;
    protected CommandContext _context;
    protected ICommandHandler _handler;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _provideInvoker = Substitute.For<ICommandProvideInvoker>();
        ProvideReturns();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _handler = Substitute.For<ICommandHandler>();
        _handler.Parameters.Returns([]);
        _context = new(_correlationId, typeof(object), new object(), [], new());
        _resolver = new(_provideInvoker);
    }

    protected void ProvideReturns(params object[] values) =>
        _provideInvoker.Invoke(Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>())
            .Returns(_ => new ValueTask<IReadOnlyList<object>>(values));

    protected void HandleHasParameters<THandlerSample>() =>
        _handler.Parameters.Returns(ParametersFor<THandlerSample>());

    protected static ParameterInfo[] ParametersFor<THandlerSample>() =>
        typeof(THandlerSample).GetMethod("Handle")!.GetParameters();

    protected ValueTask<CommandHandlerArgumentResolution> Resolve() =>
        _resolver.Resolve(_handler, _context, _serviceProvider, allowedSeverity: null);
}
