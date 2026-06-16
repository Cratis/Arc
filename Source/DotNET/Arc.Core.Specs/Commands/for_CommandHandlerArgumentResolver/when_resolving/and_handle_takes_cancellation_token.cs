// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_handle_takes_cancellation_token : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(CancellationToken cancellationToken) { }
    }

    readonly CancellationTokenSource _cancellationTokenSource = new();
    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        _context = _context with { CancellationToken = _cancellationTokenSource.Token };
        HandleHasParameters<Handler>();
        ProvideReturns();
    }

    async Task Because() => _result = await Resolve();

    void Destroy() => _cancellationTokenSource.Dispose();

    [Fact] void should_pass_the_command_context_cancellation_token() => _result.Arguments[0].ShouldEqual(_cancellationTokenSource.Token);
    [Fact] void should_not_resolve_cancellation_token_from_di() => _serviceProvider.DidNotReceive().GetService(typeof(CancellationToken));
}
