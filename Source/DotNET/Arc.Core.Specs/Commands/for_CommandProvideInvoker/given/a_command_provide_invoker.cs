// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.given;

public class a_command_provide_invoker : Specification
{
    protected CommandProvideInvoker _invoker;
    protected IServiceProvider _serviceProvider;

    void Establish()
    {
        _invoker = new();
        _serviceProvider = Substitute.For<IServiceProvider>();
    }

    protected ValueTask<IReadOnlyList<object>> Invoke(object command, CancellationToken cancellationToken = default) =>
        _invoker.Invoke(
            new CommandContext(
                CorrelationId.New(),
                command.GetType(),
                command,
                [],
                new(),
                CancellationToken: cancellationToken),
            _serviceProvider);
}
