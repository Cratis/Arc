// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_a_nested_command_requests_completion : given.a_pending_completion
{
    async Task Because()
    {
        _scope.Begin(_context);
        var nested = _context with { Values = new() };
        _scope.Begin(nested);
        var nestedResult = await _handler.Handle(nested, new CompleteStream("completion", "period"));
        await _scope.Complete(nested, nestedResult);
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_complete_only_after_the_outer_commit() => _operations.ShouldEqual(["commit", "complete"]);
}
