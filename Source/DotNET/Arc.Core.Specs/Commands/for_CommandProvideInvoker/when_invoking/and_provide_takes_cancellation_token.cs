// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_provide_takes_cancellation_token : given.a_command_provide_invoker
{
    class Command
    {
        public CancellationToken CancellationToken;

        public bool Provide(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return true;
        }
    }

    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly Command _command = new();
    IReadOnlyList<object> _result;

    async Task Because() => _result = await Invoke(_command, _cancellationTokenSource.Token);

    void Destroy() => _cancellationTokenSource.Dispose();

    [Fact] void should_invoke_provide() => _result[0].ShouldEqual(true);
    [Fact] void should_pass_the_command_context_cancellation_token() => _command.CancellationToken.ShouldEqual(_cancellationTokenSource.Token);
    [Fact] void should_not_resolve_cancellation_token_from_di() => _serviceProvider.DidNotReceive().GetService(typeof(CancellationToken));
}
